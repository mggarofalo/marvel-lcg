using static Marvel.Cards.Run.AbilityPaymentRules;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Commits window costs and starts the admitted effect with its explicit recipient.</summary>
internal static class AbilityWindowActivation
{
    internal static bool Pay(AbilityResolutionExecution execution, AbilityResolutionState cast,
        CompiledCardAbility found, PendingAbility address, Decision answer)
    {
        World world = cast.World;
        Card card = cast.Source;
        Occurrence occurrence = cast.Occurrence;
        List<GameEvent> events = cast.Events;
        IReadOnlyList<int> paying = answer.Spent;
        IReadOnlyList<int> chosen = answer.Targets;
        var costOwner = world.Agenda.Current;
        var costOccurrence = world.Agenda.Occurrence;
        var arrowPayment = AbilityCostPayment.Prepare(
            world, card, cast.Player, found.Cost, paying,
            cast.Results.ContainsKey(AbilityRecipientCommitment.PendingRecipient) ? [] : chosen,
            execution.program, execution.resourceAbilities, answer.DefinedValues,
            resourcesPaidByEvent: EffectiveCards.Kind(card, world.Facts) == CardKind.Event
                && ResourceRequirement(found.Cost, card).Length > 0);
        var eventPayment = AbilityEventPayment.Prepare(
            world, card, cast.Player, paying, found.Effect,
            execution.resourceAbilities, answer.Allocated, found.Cost);
        if (eventPayment is not null)
            cast.PaidWith(eventPayment.Commit(occurrence, events));
        execution.ApplyPayment(
            arrowPayment.Commit(execution.cardPlayAbilities, cast.Trigger, events), cast);
        if (!cast.Suspended) return true;
        execution.SuspendAfterCost(cast, address.Ordinal, costOwner, costOccurrence);
        return false;
    }

    internal static void Execute(AbilityResolutionExecution execution, AbilityResolutionState cast,
        CompiledCardAbility found, PendingAbility address)
    {
        World world = cast.World;
        Card card = cast.Source;
        Occurrence occurrence = cast.Occurrence;
        execution.Use(world, card, found, occurrence);
        if (EffectiveCards.Kind(card, world.Facts) == CardKind.Event)
            occurrence.BeginCard(card.ObjectId, [address]);
        cast.RestoreAbility(address.Ordinal, []);
        cast.TrackResolution(address.Ordinal);
        if (!AbilityRecipientCommitment.Resume(execution, cast, found)) execution.Run(found, cast);
        cast.CompleteResolution();
        execution.DiscardEvent(card, cast);
    }
}
