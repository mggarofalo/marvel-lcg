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
            world, card, cast.Player, found.Cost, execution.program, execution.resourceAbilities,
            AbilityPaymentSelection.Capture(paying,
                cast.Results.ContainsKey(AbilityRecipientCommitment.PendingRecipient) ? [] : chosen,
                answer.DefinedValues,
                resourcesPaidByEvent: EffectiveCards.Kind(card, world.Facts) == CardKind.Event
                    && (ResourceRequirement(found.Cost, card).Length + GenericResourceAmount(found.Cost)) > 0));
        var eventPayment = AbilityEventPayment.Prepare(
            world, card, cast.Player, paying, found,
            execution.resourceAbilities, answer.Allocated);
        cast.PendingPayment = new AbilityInitiationPayment(eventPayment, arrowPayment);
        if (cast.PendingPayment.Commit(execution, cast))
        {
            cast.PendingPayment = null;
            return true;
        }
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
        execution.DiscardEvent(card, cast);
        cast.CompleteResolution();
    }
}
