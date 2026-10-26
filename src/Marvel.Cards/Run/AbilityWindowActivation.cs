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
    internal static AbilityPaymentPlan Prepare(AbilityResolutionExecution execution, AbilityResolutionState cast,
        CompiledCardAbility found, Decision answer)
    {
        World world = cast.World;
        Card card = cast.Source;
        IReadOnlyList<int> paying = answer.Spent;
        IReadOnlyList<int> chosen = answer.Targets;
        AbilityEventThreatFacts.Validate(execution, cast, found);
        var arrowPayment = AbilityCostPayment.Prepare(
            world, card, cast.Player, found.Cost, execution.program, execution.resourceAbilities,
            AbilityPaymentSelection.Capture(paying,
                cast.Results.ContainsKey(AbilityRecipientCommitment.PendingRecipient) ? [] : chosen,
                answer.DefinedValues,
                resourcesPaidByEvent: EventPaysResources(world, card, found.Cost), allocations: answer.Allocations));
        var eventPayment = AbilityEventPayment.Prepare(
            world, card, cast.Player, found, execution.resourceAbilities,
            AbilityPaymentSelection.Capture(paying, [], answer.DefinedValues, allocations: answer.Allocated));
        if (eventPayment is not null)
            AbilityPaymentReceiptAdmission.Validate(found, execution.AdmissionContext(cast), eventPayment.PreviewPaid, eventPayment.Energy);
        return new AbilityInitiationPayment(eventPayment, arrowPayment);
    }

    internal static bool Pay(AbilityResolutionExecution execution, AbilityResolutionState cast,
        PendingAbility address, AbilityPaymentPlan payment)
    {
        var costOwner = cast.World.Agenda.Current;
        var costOccurrence = cast.World.Agenda.Occurrence;
        cast.PendingPayment = payment;
        if (cast.PendingPayment.Commit(execution, cast))
        {
            cast.PendingPayment = null;
            if (payment.StopsEffect)
            {
                execution.DiscardEvent(cast.Source, cast);
                return false;
            }
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
            occurrence.Resolution.BeginCard(card.ObjectId, [address]);
        cast.RestoreAbility(address.Ordinal, []);
        cast.TrackResolution(address.Ordinal);
        if (!AbilityRecipientCommitment.Resume(execution, cast, found)) execution.Run(found, cast);
        execution.DiscardEvent(card, cast);
        cast.CompleteResolution();
    }
}
