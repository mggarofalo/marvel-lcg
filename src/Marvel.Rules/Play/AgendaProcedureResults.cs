using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Propagates procedure results across their exact pending frames.</summary>
public static class AgendaProcedureResults
{
    /// <summary>Remember gained Surge on continuations owned by one exact reveal.</summary>
    /// <remarks>Pending authored abilities share the reveal's non-numeric keyword grant.
    /// A later reveal of the same physical card has a distinct occurrence and grant.</remarks>
    public static void MarkSurgeGained(this Agenda agenda, int source, Occurrence reveal)
    {
        ArgumentNullException.ThrowIfNull(reveal);
        for (int index = 0; index < agenda.items.Count; index++)
        {
            var (step, stage, occurrence) = agenda.items[index];
            AgendaOperation updated = step.Operation switch
            {
                AbilityContinuationAgendaOperation ability when step.Subject == source
                    && ReferenceEquals(ability.Origin, reveal) =>
                    ability with { SurgeGained = true },
                CharacterAttackOperation { Attack: { } attack } operation when attack.Source == source
                    && ReferenceEquals(attack.Origin, reveal) =>
                    operation with { Attack = attack with { SurgeGained = true } },
                CharacterThwartOperation { Thwart: { } thwart } operation when thwart.Source == source
                    && ReferenceEquals(thwart.Origin, reveal) =>
                    operation with { Thwart = thwart with { SurgeGained = true } },
                _ => step.Operation,
            };
            agenda.items[index] = (step with { Operation = updated }, stage, occurrence);
        }
    }

    /// <summary>Persist one per-card result on every frame of a rules procedure.</summary>
    /// <remarks>
    /// Correctness cannot depend on several steps retaining one shared
    /// collection reference. The containing occurrence identifies the frames;
    /// the dictionary spelling is an engine representation choice.
    /// </remarks>
    public static void RecordProcedureAmount(this Agenda agenda, Occurrence procedure, int card, long amount)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        for (int index = 0; index < agenda.items.Count; index++)
        {
            var (step, stage, occurrence) = agenda.items[index];
            if (step.Operation is not IndirectDamageOperation operation
                || !ReferenceEquals(operation.Origin, procedure))
            {
                continue;
            }

            var amounts = operation.Amounts is { } existing
                ? new Dictionary<int, long>(existing)
                : [];
            amounts[card] = amount;
            agenda.items[index] = (step with { Operation = operation with { Amounts = amounts } }, stage, occurrence);
        }
    }

}
