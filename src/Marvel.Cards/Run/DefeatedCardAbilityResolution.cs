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

        // Damage step 7 evaluates this exact defeat while the card remains in
        // play. Its local window carries the defeated subject; the containing
        // occurrence retains the same triggering fact for response discovery.
        // The spent key includes that fact, so separate defeats in one shared
        // window each give an answering ability its own opportunity.
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
                    TriggeringDefeat = defeated,
                };
                execution.TrackResolution(cast, ability);
                execution.Run(ability, cast);
                cast.CompleteResolution();
            }
            return true;
        }

        var own = written.Select((_, ordinal) => new PendingAbility(
            card.ObjectId, AbilityType.WhenDefeated, card.Owner, ordinal)
            { TriggeringDefeat = defeated });
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
            occurrence.Trigger(WindowKind.Interrupt, next);
            spent?.Trigger(WindowKind.Interrupt, next);
            ActivityBatches.Append(events, execution.Resolve(world, occurrence, next, [], []));
            waiting.Remove(next);
        }

        return true;
    }

    internal static void SuspendCardDefeated(this AbilityResolutionExecution execution,
        World world, Card card, string trigger, Occurrence occurrence,
        Defeated defeated, IReadOnlyList<PendingAbility> pending)
    {
        var step = new PhaseStep(
            new DefeatAbilityOperation(DefeatAbilityKind.ChooseCardDefeatedAbility,
                new DefeatCause(Trigger: trigger, Verb: defeated.How, By: defeated.By),
                new DefeatWindow(occurrence, [.. pending])),
            world.Agenda.Current?.Round ?? 0,
            7,
            Subject: card.ObjectId,
            Seat: card.Owner >= 0 ? card.Owner : world.FirstPlayer,
            Plan: true);

        if (world.Agenda.Occurrence is { } parent)
        {
            var owner = world.Agenda.Current
                ?? throw new InvalidOperationException("a defeat occurrence has no owning frame");
            world.Agenda.ThenContinuation(step, parent);
            world.Agenda.BeforeResponses(owner, parent);
        }
        else
        {
            world.Agenda.Add(step);
        }
    }
}
