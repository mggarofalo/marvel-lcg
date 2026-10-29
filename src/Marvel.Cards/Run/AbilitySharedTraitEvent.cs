using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// A bounded event branch: one ordinary Action and no arrow cost. Other event
// shapes need action-address and combined-cost admission before they can play.
internal static class AbilitySharedTraitEvent
{
    internal static PendingAbility Action(AbilityProgram program, World world, Card card, int player)
    {
        var actions = AbilityProgramQueries.ActiveOn(world, program, card)
            .Where(ability => ability.Trigger.Timing == AbilityType.Action).ToArray();
        if (actions.Length != 1 || actions[0].Cost is not null
            || AbilityEffectStructure.PaidResourceQueries(actions[0].Effect).Any())
            throw new RulesNotImplementedException("shared-trait event play requires one Action without an arrow cost or paid-resource-dependent effect");
        return new PendingAbility(card.ObjectId, AbilityType.Action, player, 0);
    }

    internal static Affordance? Offer(World world, int player, Card card, Card source,
        AbilityProgram program, IResourceCardAbilities resources, bool excludeSource)
    {
        var reduction = new ScopedCardCostReduction(world, world.Seats[player], card, 1);
        var written = AbilityProgramQueries.ActiveOn(world, program, card);
        if (!written.Any(ability => ability.Trigger.Timing == AbilityType.Action)) return null;
        _ = Action(program, world, card, player);
        var ability = written.Single(candidate => candidate.Trigger.Timing == AbilityType.Action);
        var queries = new AbilityOfferQueries(program, resources);
        if (!Resources.HasPlayableCost(card.FaceId, world.Facts)
            || queries.ActionAdmission(world, card, ability, player, null, reduction: reduction) is null)
            return null;
        var price = AbilityPaymentPricing.CombinedPrice(world, card, player, ability, resources, reduction)!;
        var generators = price.Generators.Where(item => !excludeSource || item.Effect != source.ObjectId).ToArray();
        if (ResourcePayment.Allocate(price, generators.Select(item => item.Effect).ToArray()) is null) return null;
        return new Affordance(card.ObjectId, AbilityStructuralExecution.ChooseVerb, card.ObjectId, player,
            ability.Name, Costs: [price], Description: AbilitySharedTraitPlay.PlayDescription(world, program, card)
                + (AbilityEffectDescription.Summary(ability.Effect) is { } effect ? $" {effect}" : string.Empty))
            { PlaysCard = true, SelectionSourceId = source.ObjectId,
                DeferredTargetSelection = ability.Effect is AbilityEffect.ChooseCard };
    }

    internal static void Validate(AbilityStructuralContext context, Card card, Decision input,
        ScopedCardCostReduction reduction)
    {
        var world = context.Expressions.World;
        _ = Action(context.Program, world, card, context.Player);
        var ability = AbilityProgramQueries.ActiveOn(world, context.Program, card)
            .Single(candidate => candidate.Trigger.Timing == AbilityType.Action);
        var queries = new AbilityOfferQueries(context.Program, context.ResourceAbilities);
        if (queries.ActionAdmission(world, card, ability, context.Player, null, reduction: reduction) is null)
            throw new ArgumentException("the event action is no longer legal", nameof(input));
        _ = AbilityEventPayment.Prepare(world, card, context.Player, ability, context.ResourceAbilities,
            AbilityPaymentSelection.Capture(input.Spent, [], input.Values, allocations: input.Allocations), reduction);
    }
}
