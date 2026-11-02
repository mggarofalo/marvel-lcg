using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// An event in a scoped play is played by triggering one of its Actions
// (rr:player-turn.5.d), with its arrow cost paid together with its reduced
// resource cost (rr:cost.5) through the ordinary Action payment. An Interrupt or
// Response on an event can be triggered only by its own triggering condition
// (rr:initiating-abilities step 2), so it is not offered here.
internal static class AbilityScopedEvent
{
    internal static PendingAbility Action(AbilityProgram program, IResourceCardAbilities resources,
        World world, Card card, ScopedCardCostReduction reduction, IReadOnlyList<int>? costSelection = null)
    {
        var admitted = Admitted(program, resources, world, card, reduction, costSelection).ToArray();
        // rr:event.1: "If an event has more than one triggered ability on it, the player
        // playing it chooses one of those abilities to trigger when playing that event."
        if (admitted.Length > 1)
            throw new RulesNotImplementedException(
                $"playing '{card.FaceId}' from a scoped play chooses between its Actions, which is not implemented");
        if (admitted.Length == 0)
            throw new ArgumentException($"'{card.FaceId}' has no Action that can be triggered now", nameof(card));
        return new PendingAbility(card.ObjectId, AbilityType.Action, reduction.Player, admitted[0].Ordinal);
    }

    // The event's Actions that can be initiated now at this price, with their ordinal among its
    // Actions. Whether the resources can be found is the offer's allocation check.
    private static IEnumerable<(CompiledCardAbility Ability, int Ordinal)> Admitted(AbilityProgram program,
        IResourceCardAbilities resources, World world, Card card, ScopedCardCostReduction reduction,
        IReadOnlyList<int>? costSelection)
    {
        var queries = new AbilityOfferQueries(program, resources);
        int player = reduction.Player;
        int ordinal = 0;
        foreach (var ability in AbilityProgramQueries.ActiveOn(world, program, card))
        {
            if (ability.Trigger.Timing != AbilityType.Action) continue;
            if (AbilityPaymentRules.Payable(world, card, player, ability.Cost, program, resources)
                && queries.ActionAdmission(world, card, ability, player, null, costSelection, reduction) is not null)
                yield return (ability, ordinal);
            ordinal++;
        }
    }

    internal static Affordance? Offer(World world, Card card, Card source,
        AbilityProgram program, IResourceCardAbilities resources, bool excludeSource, ScopedCardCostReduction reduction)
    {
        int player = reduction.Player;
        if (!Resources.HasPlayableCost(card.FaceId, world.Facts)
            || !Admitted(program, resources, world, card, reduction, null).Any()) return null;
        var pending = Action(program, resources, world, card, reduction);
        var ability = AbilityProgramQueries.ActiveOn(world, program, card)
            .Where(candidate => candidate.Trigger.Timing == AbilityType.Action).ElementAt(pending.Ordinal);
        var queries = new AbilityOfferQueries(program, resources);
        var price = AbilityAffordanceDescription.DeclaredPrice(queries, world, card, ability, player,
            AbilityPaymentPricing.CombinedPrice(world, card, player, ability, resources, reduction))!;
        var generators = price.Generators.Where(item => !excludeSource || item.Effect != source.ObjectId).ToArray();
        var values = price.VariableRequests.ToDictionary(variable => variable.Name, variable => variable.Min, StringComparer.Ordinal);
        if (ResourcePayment.Allocate(price, generators.Select(item => item.Effect).ToArray(), values) is null) return null;
        return new Affordance(card.ObjectId, AbilityStructuralExecution.ChooseVerb, card.ObjectId, player,
            ability.Name, Targets: AbilityAffordanceDescription.AdmittedCostTargets(queries, world, card, ability, player, reduction),
            Costs: [price], Description: AbilityScopedPlay.PlayDescription(world, program, card, source, reduction.Amount)
                + (AbilityEffectDescription.Summary(ability.Effect) is { } effect ? $" {effect}" : string.Empty))
            { PlaysCard = true, SelectionSourceId = source.ObjectId,
                DeferredTargetSelection = ability.Effect is AbilityEffect.ChooseCard };
    }

    // Rechecks the Action's initiation and both payments before anything moves.
    internal static void Validate(AbilityStructuralContext context, Card card, Decision input,
        ScopedCardCostReduction reduction)
    {
        var world = context.Expressions.World;
        var pending = Action(context.Program, context.ResourceAbilities, world, card, reduction, input.Targets);
        var ability = AbilityProgramQueries.ActiveOn(world, context.Program, card)
            .Where(candidate => candidate.Trigger.Timing == AbilityType.Action).ElementAt(pending.Ordinal);
        _ = AbilityCostPayment.Prepare(world, card, context.Player, ability.Cost, context.Program, context.ResourceAbilities,
            AbilityPaymentSelection.Capture(input.Spent, input.Targets, input.Values,
                resourcesPaidByEvent: AbilityPaymentRules.EventPaysResources(world, card, ability.Cost), allocations: input.Allocations));
        _ = AbilityEventPayment.Prepare(world, card, context.Player, ability, context.ResourceAbilities,
            AbilityPaymentSelection.Capture(input.Spent, [], input.Values, allocations: input.Allocations), reduction);
    }
}
