using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Participants of the innermost attack whose resolution is still open.</summary>
public sealed record ActiveAttack(int Source, int Target)
{
    /// <summary>Stable identity of the owning activation or character attack occurrence.</summary>
    public Events.AttackIdentity? Identity { get; init; }
    /// <summary>Reads attack ownership without advancing an occurrence or predicting its result.</summary>
    public static ActiveAttack? From(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return Open(world).FirstOrDefault();
    }

    internal static IEnumerable<ActiveAttack> Open(World world)
    {
        if (world.IsOver) yield break;
        // Engine representation choice: an owner remains active through its response
        // window and nested work. Retained last-attack facts alone do not own a lifetime.
        foreach (var (step, _, occurrence) in world.Agenda.items)
            foreach (var owner in StepOwners(world, step, occurrence)) yield return owner;
        if (world.Attack is { } attack) yield return new ActiveAttack(attack.Enemy, attack.Target)
            { Identity = world.Activation is { } active ? new(active.Id, true) : null };
    }

    private static IEnumerable<ActiveAttack> StepOwners(World world, PhaseStep step, Occurrence? occurrence)
    {
        if (CharacterOwner(world, step, occurrence) is { } character) yield return character;
        if (step.What == Steps.EndAttack && world.Attack is { } pending)
            yield return new ActiveAttack(pending.Enemy, pending.Target) { Identity = new(step.ActivationId, true) };
        if (FinishedOwner(world, occurrence) is { } finished) yield return finished;
        if (FinishedOwner(world, (step.Operation as CharacterAttackOperation)?.Attack?.Origin ?? (step.Operation as AbilityContinuationAgendaOperation)?.Origin) is { } enclosing)
            yield return enclosing;
    }

    private static ActiveAttack? CharacterOwner(World world, PhaseStep step, Occurrence? occurrence)
    {
        if (step.Operation is LateAttackRecipientOperation late) return LateRecipientOwner(world, late, occurrence);
        if (step.Operation is not CharacterAttackOperation { Attack: { } attack }) return null;
        return occurrence is not null
            ? new ActiveAttack(attack.Attacker, attack.Enemy)
                { Identity = new(occurrence?.Id ?? step.OccurrenceId ?? Moment.Id(step.Round, step.Number, step.Index), false) } : null;
    }

    // While a late recipient's attack-initiated window is open, the begun player
    // attack is attacking that enemy (rr:attack-player-ability-type.5), so it is
    // the attack's target for the length of the window. The identity stays the
    // owning character attack's.
    private static ActiveAttack? LateRecipientOwner(World world, LateAttackRecipientOperation late, Occurrence? occurrence)
    {
        if (occurrence is null) return null;
        var owner = world.Agenda.items.FirstOrDefault(item => item.Occurrence is not null
            && item.Step.Operation is CharacterAttackOperation);
        return owner.Occurrence is { } initiation
            ? new ActiveAttack(late.Attacker, late.Recipient) { Identity = new(initiation.Id, false) } : null;
    }

    private static ActiveAttack? FinishedOwner(World world, Occurrence? occurrence)
    {
        if (occurrence is null || !occurrence.Is(Steps.AttackEnds)
            || world.FinishedAttack is not { } attack || occurrence.Actor != attack.Enemy) return null;
        return new ActiveAttack(attack.Enemy, attack.Target)
            { Identity = world.FinishedActivation is { } finished ? new(finished.Id, true) : null };
    }
}
