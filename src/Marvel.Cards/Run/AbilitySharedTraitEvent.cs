using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// A bounded event branch: one ordinary Action and no arrow cost. Other event
// shapes need action-address and combined-cost admission before they can play.
// A play ignoring the resource cost pays nothing, so a paid-resource query reads
// that empty payment (rr:ignore.1); a reduced play's paid queries are not modelled.
internal static class AbilitySharedTraitEvent
{
    internal static PendingAbility Action(AbilityProgram program, World world, Card card, int player, bool ignoresCost)
    {
        if (Unsupported(program, world, card, ignoresCost) is { } reason) throw new RulesNotImplementedException(reason);
        return new PendingAbility(card.ObjectId, AbilityType.Action, player, 0);
    }

    private static string? Unsupported(AbilityProgram program, World world, Card card, bool ignoresCost)
    {
        var actions = AbilityProgramQueries.ActiveOn(world, program, card)
            .Where(ability => ability.Trigger.Timing == AbilityType.Action).ToArray();
        return actions.Length != 1 || actions[0].Cost is not null
            || !ignoresCost && AbilityEffectStructure.PaidResourceQueries(actions[0].Effect).Any()
            ? $"playing '{card.FaceId}' from a scoped play requires one Action without an arrow cost"
                + (ignoresCost ? string.Empty : " or paid-resource-dependent effect")
            : null;
    }

    internal static Affordance? Offer(World world, Card card, Card source,
        AbilityProgram program, IResourceCardAbilities resources, bool excludeSource, ScopedCardCostReduction reduction)
    {
        int player = reduction.Player;
        var written = AbilityProgramQueries.ActiveOn(world, program, card);
        if (!written.Any(ability => ability.Trigger.Timing == AbilityType.Action)) return null;
        bool ignores = reduction.IgnoresResourceCost;
        // An unsupported shape is offered when the play may be legal and raises when chosen.
        if (ignores && Unsupported(program, world, card, ignores) is not null)
            return new Affordance(card.ObjectId, AbilityStructuralExecution.ChooseVerb, card.ObjectId, player,
                card.FaceId, Description: AbilitySharedTraitPlay.PlayDescription(world, program, card, source, ignores))
            { PlaysCard = true, SelectionSourceId = source.ObjectId };
        _ = Action(program, world, card, player, ignores);
        var ability = written.Single(candidate => candidate.Trigger.Timing == AbilityType.Action);
        var queries = new AbilityOfferQueries(program, resources);
        if (!Resources.HasPlayableCost(card.FaceId, world.Facts)
            || queries.ActionAdmission(world, card, ability, player, null, reduction: reduction) is null)
            return null;
        var price = AbilityPaymentPricing.CombinedPrice(world, card, player, ability, resources, reduction)!;
        var generators = price.Generators.Where(item => !excludeSource || item.Effect != source.ObjectId).ToArray();
        if (ResourcePayment.Allocate(price, generators.Select(item => item.Effect).ToArray()) is null) return null;
        return new Affordance(card.ObjectId, AbilityStructuralExecution.ChooseVerb, card.ObjectId, player,
            ability.Name, Costs: [price], Description: AbilitySharedTraitPlay.PlayDescription(world, program, card, source, ignores)
                + (AbilityEffectDescription.Summary(ability.Effect) is { } effect ? $" {effect}" : string.Empty))
            { PlaysCard = true, SelectionSourceId = source.ObjectId,
                DeferredTargetSelection = ability.Effect is AbilityEffect.ChooseCard };
    }

    internal static void Validate(AbilityStructuralContext context, Card card, Decision input,
        ScopedCardCostReduction reduction, bool ignoresCost)
    {
        var world = context.Expressions.World;
        _ = Action(context.Program, world, card, context.Player, ignoresCost);
        var ability = AbilityProgramQueries.ActiveOn(world, context.Program, card)
            .Single(candidate => candidate.Trigger.Timing == AbilityType.Action);
        var queries = new AbilityOfferQueries(context.Program, context.ResourceAbilities);
        if (queries.ActionAdmission(world, card, ability, context.Player, null, reduction: reduction) is null)
            throw new ArgumentException("the event action is no longer legal", nameof(input));
        _ = AbilityEventPayment.Prepare(world, card, context.Player, ability, context.ResourceAbilities,
            AbilityPaymentSelection.Capture(input.Spent, [], input.Values, allocations: input.Allocations), reduction);
    }
}
