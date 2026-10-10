using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Resolves and orders abilities answering a completed card defeat.</summary>
internal static class DefeatedCardAbilityResolution
{
    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> WhenCardDefeated(this AbilityResolutionExecution execution, World world, Card card, Defeated defeated)
    {
        var events = new List<GameEvent>();
        _ = execution.WhenCardDefeated(world, card, defeated, Steps.CardDefeated, events);
        return events;
    }

    /// <inheritdoc/>
    internal static bool WhenCardDefeated(this AbilityResolutionExecution execution,
        World world, Card card, Defeated defeated, string trigger,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(defeated);

        var written = execution.On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenDefeated)
            .ToList();

        // **The printed check gates the complaint, not the run.** Nothing in
        // the printed attributes records a "When Defeated", so an unwritten one
        // and a card that has none look identical from here -- but that is only
        // a question when there is nothing written. Asking it first would let
        // the text box veto authored data, which is the wrong way round: the
        // data is what the engine runs.
        if (written.Count == 0 && world.Facts.HasWhenDefeated(card.FaceId))
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was defeated and prints a 'When Defeated' "
                + "ability that no ability data is written for");
        }

        // **Two occurrences, and each is asked what only it can answer.**
        //
        // This one is built here because the matching needs the defeated card:
        // "when **attached minion** is defeated" is a claim about which card
        // died, while the occurrence the defeat joined keeps the cause. An
        // attack carries its actor and target separately. This occurrence also
        // carries the provenance because "the player who defeated this scheme"
        // is on the card and not on the board.
        //
        // What it cannot answer is `rr:triggering-condition.1`, "each
        // **Interrupt** ability can only be triggered once per occurrence of
        // its triggering condition". The occurrence there is the one on the
        // agenda: it is what lasts, it is what a still-open interrupt window is
        // polling, and it is where a second defeat in the same moment would
        // find an ability already spent. This one is made fresh on every call
        // and would forget all of that.
        var occurrence = new Occurrence(
            0, [Steps.CardDefeated], Subject: card.ObjectId, Player: card.Owner);
        occurrence.Also(defeated);

        var spent = world.Agenda.Occurrence;
        var elsewhere = execution.Answering(world, card, occurrence, spent);
        if (elsewhere.Count == 0)
        {
            // `rr:when-defeated-abilities.2` says all abilities on the defeated
            // card resolve. Their printed/data order is already authoritative;
            // the cross-card ordering question from `rr:forced.5` does not
            // arise until another card answers the same defeat.
            foreach (var ability in written)
            {
                var cast = new AbilityResolutionState(world, card, occurrence, card.Owner, events)
                {
                    Tier = ability.Trigger.Timing,
                };
                execution.TrackResolution(cast, ability);
                execution.Run(ability, cast);
                cast.CompleteResolution();
            }
            return true;
        }

        var own = written.Select((_, ordinal) => new PendingAbility(
            card.ObjectId, AbilityType.WhenDefeated, card.Owner, ordinal));
        var waiting = own.Concat(elsewhere).ToList();
        while (waiting.Count > 0)
        {
            var mandatory = waiting
                .Where(ability => AbilityTypes.IsMandatory(ability.Type))
                .ToList();
            var offered = mandatory.Count > 0
                ? mandatory
                : waiting.Where(ability => !AbilityTypes.IsMandatory(ability.Type)).ToList();
            if (offered.Count > 1 || mandatory.Count == 0)
            {
                execution.SuspendCardDefeated(
                    world, card, trigger, occurrence, defeated, offered);
                return false;
            }

            var next = offered[0];
            occurrence.Trigger(WindowKind.Interrupt, next.Card);
            spent?.Trigger(WindowKind.Interrupt, next.Card);
            events.AddRange(execution.Resolve(world, occurrence, next, [], []));
            waiting.Remove(next);
        }

        return true;
    }

    internal static void SuspendCardDefeated(this AbilityResolutionExecution execution,
        World world, Card card, string trigger, Occurrence occurrence,
        Defeated defeated, IReadOnlyList<PendingAbility> pending)
    {
        occurrence.Also(defeated);
        var step = new PhaseStep(
            Steps.ChooseCardDefeatedAbility,
            world.Agenda.Current?.Round ?? 0,
            7,
            Subject: card.ObjectId,
            Seat: card.Owner >= 0 ? card.Owner : world.FirstPlayer,
            Plan: true,
            ProcedureAbilities: [.. pending],
            ProcedureOccurrence: occurrence,
            ProcedureTrigger: trigger,
            ProcedureVerb: defeated.How,
            ProcedureBy: defeated.By);

        if (world.Agenda.Occurrence is { } parent)
        {
            world.Agenda.ThenContinuation(step, parent);
            world.Agenda.BeforeResponses(parent);
        }
        else
        {
            world.Agenda.Add(step);
        }
    }
}
