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
        World world, Occurrence occurrence, Occurrence? spent)
    {
        var tiers = AbilityWindow.Tiers(
            execution.Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(pending => spent?.MayTrigger(WindowKind.Interrupt, pending) ?? true),
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

    internal static AbilityExecutionResult Act(this AbilityResolutionExecution execution,
        World world, AbilityExecution invocation, Decision answer)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(invocation);
        ArgumentNullException.ThrowIfNull(answer);
        world.Agenda.Executions.RequireActive(invocation);
        var ability = invocation.Ability;
        var occurrence = invocation.Occurrence;
        var paying = answer.Spent;
        var chosen = answer.Targets;
        var values = answer.Values;
        var allocations = answer.Allocations;

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

        cast.EnterExecution(invocation);

        // The wire only names an affordance; it is not authority to use one
        // after its legality has changed. Re-run the same initiation checks
        // that produced Actions before any cost moves a card or exhausts a
        // game element. `rr:cost.6` and `rr:event.3` make target availability
        // part of whether the cost may be paid at all.
        var admission = execution.offerQueries.ActionAdmission(
            world, card, found, ability.Player, occurrence, chosen);
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
        var recipientChoice = AbilityRecipientCommitment.Validate(execution, cast, found, chosen);
        AbilityEventThreatFacts.Validate(execution, cast, found);
        var arrowPayment = AbilityCostPayment.Prepare(
            world, card, cast.Player, found.Cost, execution.program, execution.resourceAbilities,
            AbilityPaymentSelection.Capture(paying, recipientChoice is null ? chosen : [], values,
                resourcesPaidByEvent: EventPaysResources(world, card, found.Cost), allocations: allocations));
        var eventPayment = AbilityEventPayment.Prepare(
            world, card, cast.Player, found, execution.resourceAbilities,
            AbilityPaymentSelection.Capture(paying, [], values, allocations: allocations));
        // rr:cost.5: costs "must be paid simultaneously". Capture the chosen
        // physical incarnation before exhaustion and retain it through cost work.
        if (AbilityCostSelection.DiscardChoice(found.Cost) is not null)
            cast.Choose(world.Cards[chosen.Single()]);
        cast.TrackResolution(ability.Ordinal);
        if (eventPayment is not null)
            AbilityPaymentReceiptAdmission.Validate(found, execution.AdmissionContext(cast), eventPayment.PreviewPaid, eventPayment.Energy);
        cast.PendingPayment = new AbilityInitiationPayment(eventPayment, arrowPayment);
        if (!CommitPayment(execution, cast, ability.Ordinal,
                recipientChoice is null ? null : chosen[0]))
            return invocation.Report(events);
        execution.Use(world, card, found, occurrence);
        if (EffectiveCards.Kind(card, world.Facts) == CardKind.Event)
        {
            occurrence.Resolution.BeginCard(card.ObjectId, [ability]);
        }
        cast.RestoreAbility(ability.Ordinal, []);
        cast.TrackResolution(ability.Ordinal);
        if (recipientChoice is not null)
            AbilityRecipientCommitment.Resolve(execution, cast, recipientChoice, chosen[0]);
        else execution.Run(found, cast);
        execution.DiscardEvent(card, cast);
        cast.CompleteResolution();
        return invocation.Report(events);
    }

    private static bool CommitPayment(AbilityResolutionExecution execution,
        AbilityResolutionState cast, int ordinal, int? recipient)
    {
        var costOwner = cast.World.Agenda.Current;
        var costOccurrence = cast.World.Agenda.Occurrence;
        cast.PendingPayment!.Commit(execution, cast);
        if (cast.Suspended)
        {
            if (recipient is { } target)
                cast.Results[AbilityRecipientCommitment.PendingRecipient] = target;
            execution.SuspendAfterCost(cast, ordinal, costOwner, costOccurrence);
            cast.CompleteResolution();
            return false;
        }
        if (cast.PendingPayment.StopsEffect)
        {
            cast.PendingPayment = null;
            execution.DiscardEvent(cast.Source, cast);
            cast.CompleteResolution();
            return false;
        }
        cast.PendingPayment = null;
        return true;
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
