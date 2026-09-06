using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Program-backed constant queries shared by live resolution and read-only
// admission. Their inputs are immutable definitions plus board facts, never a
// runner callback.
internal static class AbilityProgramQueries
{
    internal static IReadOnlyList<CompiledCardAbility> On(AbilityProgram program, Card card) =>
        FacedownDrones.Is(card) ? [] : program.On(card.FaceId);

    internal static CardCounterPool? CounterPool(World world, AbilityProgram program, Card card)
    {
        if (program.CounterPools.GetValueOrDefault(card.FaceId) is { } authored) return authored;
        var (count, type) = Reveal.Uses(world.Facts.Attributes(card.FaceId));
        return count > 0 ? new CardCounterPool(type, checked((int)count), Uses: true) : null;
    }

    internal static bool CanReady(World world, AbilityProgram program, Card target)
    {
        foreach (var card in world.Areas.Where(area => DeckTypes.IsInPlay(area.Type))
                     .SelectMany(area => area.Cards))
        {
            if (FacedownDrones.Is(card)) continue;
        foreach (var ability in On(program, card).Where(ability =>
                     ability.Trigger.Timing == AbilityType.Constant))
        {
            var bindings = new AbilityQueryContext(
                world, card, new Occurrence(0, []),
                AbilityCardQueries.ControllerOf(world, card), card.Incarnation,
                null, null, null, []);
            var selectors = new AbilitySelectorEvaluation(bindings, null, program);
            var expressions = new AbilityExpressionEvaluation(
                new AbilityExpressionContext(
                    bindings, ImmutableDictionary<string, long>.Empty, [], string.Empty,
                    -1, false, null), selectors);
            if (ProhibitsReady(ability.Effect, target, selectors, expressions)) return false;
        }
        }
        return true;
    }

    internal static bool CanTakeDamage(
        World world, AbilityProgram program, Card target, Card source)
    {
        if (!DeckTypes.IsInPlay(target.Area.Type) || FacedownDrones.Is(target)) return true;
        foreach (var ability in On(program, target).Where(ability =>
                     ability.Trigger.Timing == AbilityType.Constant))
        {
            var bindings = new AbilityQueryContext(
                world, target, new Occurrence(0, []),
                AbilityCardQueries.ControllerOf(world, target), target.Incarnation,
                null, null, null, []);
            var expressions = new AbilityExpressionEvaluation(
                new AbilityExpressionContext(
                    bindings, ImmutableDictionary<string, long>.Empty, [], string.Empty,
                    -1, false, null),
                new AbilitySelectorEvaluation(bindings));
            if (ProhibitsDamage(ability.Effect, world, source, expressions)) return false;
        }
        return true;
    }

    internal static bool CanRemoveThreat(
        World world, AbilityProgram program, Card scheme, int ignoredSource = -1)
    {
        foreach (var card in world.Cards.Where(card =>
            card.ObjectId != ignoredSource && DeckTypes.IsInPlay(card.Area.Type)))
        {
            foreach (var ability in On(program, card).Where(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
            {
                var bindings = new AbilityQueryContext(
                    world, card, new Occurrence(0, []),
                    AbilityCardQueries.ControllerOf(world, card), card.Incarnation,
                    null, null, null, []);
                var selectors = new AbilitySelectorEvaluation(bindings, null, program);
                var expressions = new AbilityExpressionEvaluation(
                    new AbilityExpressionContext(
                        bindings, ImmutableDictionary<string, long>.Empty, [],
                        string.Empty, -1, false, null), selectors);
                if (ProhibitsThreatRemoval(
                    ability.Effect, scheme, selectors, expressions)) return false;
            }
        }
        return true;
    }

    private static bool ProhibitsThreatRemoval(
        AbilityEffect effect, Card scheme, AbilitySelectorEvaluation selectors,
        AbilityExpressionEvaluation expressions) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.Any(step =>
            ProhibitsThreatRemoval(step, scheme, selectors, expressions)),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(step =>
            ProhibitsThreatRemoval(step, scheme, selectors, expressions)),
        AbilityEffect.Conditional conditional => (expressions.Test(conditional.Test)
                ? conditional.Then : conditional.Else) is { } branch
            && ProhibitsThreatRemoval(branch, scheme, selectors, expressions),
        AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.PreventThreatRemoval } prohibition =>
            selectors.Find(prohibition.Selection)?.ObjectId == scheme.ObjectId,
        _ => false,
    };

    private static bool ProhibitsDamage(
        AbilityEffect effect, World world, Card source, AbilityExpressionEvaluation expressions) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.Any(step =>
            ProhibitsDamage(step, world, source, expressions)),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(step =>
            ProhibitsDamage(step, world, source, expressions)),
        AbilityEffect.Conditional conditional => (expressions.Test(conditional.Test)
                ? conditional.Then : conditional.Else) is { } branch
            && ProhibitsDamage(branch, world, source, expressions),
        AbilityEffect.PreventDamageFrom prohibition => world.Facts.Kind(source.FaceId)
            == prohibition.SourceKind && Rules.State.Traits.Has(
                world, source, prohibition.SourceTrait, world.Facts),
        AbilityEffect.PreventDamageWhile prohibition => expressions.Test(prohibition.Condition),
        _ => false,
    };

    private static bool ProhibitsReady(
        AbilityEffect effect, Card target, AbilitySelectorEvaluation selectors,
        AbilityExpressionEvaluation expressions) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.Any(step =>
            ProhibitsReady(step, target, selectors, expressions)),
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(step =>
            ProhibitsReady(step, target, selectors, expressions)),
        AbilityEffect.Conditional conditional => (expressions.Test(conditional.Test)
                ? conditional.Then : conditional.Else) is { } branch
            && ProhibitsReady(branch, target, selectors, expressions),
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.PreventReady } prohibition =>
            selectors.Find(prohibition.Selection)?.ObjectId == target.ObjectId,
        _ => false,
    };
}
