using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityPaymentRules;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionActions
{
    internal static IReadOnlyList<PendingAbility> Answering(this AbilityResolutionExecution execution,
        World world, Card card, Occurrence occurrence, Occurrence? spent)
    {
        var tiers = AbilityWindow.Tiers(
            execution.Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(pending => pending.Card != card.ObjectId)
                .Where(pending => spent?.MayTrigger(WindowKind.Interrupt, pending.Card) ?? true),
            WindowKind.Interrupt,
            occurrence);

        foreach (var tier in tiers)
        {
            var (mandatory, optional) = AbilityWindow.Split(tier);
            if (mandatory.Count > 0 || optional.Count > 0)
            {
                return mandatory.Count > 0 ? mandatory : optional;
            }
        }

        return [];
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> Act(this AbilityResolutionExecution execution,
        World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null)
        => execution.Act(
            world, ability, paying, chosen,
            new Occurrence(
                0, [Steps.TurnAction], Subject: ability.Card, Player: ability.Player),
            values, allocations);

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> Act(this AbilityResolutionExecution execution,
        World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen, Occurrence occurrence,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(paying);
        ArgumentNullException.ThrowIfNull(chosen);
        ArgumentNullException.ThrowIfNull(occurrence);

        var card = world.Cards[ability.Card];
        var found = execution.Pending(card, ability);

        var events = new List<GameEvent>();
        var cast = new AbilityResolutionState(
            world,
            card,
            occurrence,
            ability.Player,
            events)
        {
            Tier = found.Trigger.Timing,
        };

        // The wire only names an affordance; it is not authority to use one
        // after its legality has changed. Re-run the same initiation checks
        // that produced Actions before any cost moves a card or exhausts a
        // game element. `rr:cost.6` and `rr:event.3` make target availability
        // part of whether the cost may be paid at all.
        var admission = execution.offerQueries.ActionAdmission(
            world, card, found, ability.Player, occurrence);
        if (admission is null)
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' cannot initiate this action in the current state");
        }
        foreach (var thwart in admission.CrisisIgnoringThwarts)
        {
            cast.ValidateCrisisIgnoringThwart(thwart);
        }
        if (!CounterCostsPayable(world, card, ability.Player, found.Cost))
        {
            throw new RulesNotImplementedException(
                $"'{card.FaceId}' can no longer pay this ability's counter cost");
        }

        // `rr:initiating-abilities` keeps the steps apart, and step 5 pays
        // before step 6 resolves.
        var costOwner = world.Agenda.Current;
        var costOccurrence = world.Agenda.Occurrence;
        var arrowPayment = AbilityCostPayment.Prepare(
            world, card, cast.Player, found.Cost, paying, chosen,
            execution.program, execution.resourceAbilities, values,
            resourcesPaidByEvent: world.Facts.Kind(card.FaceId) == CardKind.Event
                && ResourceRequirement(found.Cost, card).Length > 0);
        var eventPayment = AbilityEventPayment.Prepare(
            world, card, cast.Player, paying, found.Effect, execution.resourceAbilities,
            allocations, found.Cost);
        if (eventPayment is not null)
        {
            cast.PaidWith(eventPayment.Commit(occurrence, events));
        }
        execution.ApplyPayment(arrowPayment.Commit(execution.cardPlayAbilities, cast.Trigger, events), cast);
        if (cast.Suspended)
        {
            execution.SuspendAfterCost(cast, ability.Ordinal, costOwner, costOccurrence);
            return events;
        }
        execution.Use(world, card, found, occurrence);
        if (world.Facts.Kind(card.FaceId) == CardKind.Event)
        {
            occurrence.BeginCard(card.ObjectId, [ability]);
        }
        cast.RestoreAbility(ability.Ordinal, []);
        cast.TrackResolution(ability.Ordinal);
        execution.Run(found, cast);
        cast.CompleteResolution();
        execution.DiscardEvent(card, cast);
        return events;
    }

    /// <summary>The exact same-timing ability named by a pending ordinal.</summary>
    internal static CompiledCardAbility Pending(this AbilityResolutionExecution execution, Card card, PendingAbility pending) =>
        execution.On(card)
            .Where(candidate => candidate.Trigger.Timing == pending.Type)
            .ElementAtOrDefault(pending.Ordinal)
        ?? throw new AbilityException(
            $"card '{card.FaceId}' has no '{pending.Type}' ability at ordinal "
            + pending.Ordinal);
}
