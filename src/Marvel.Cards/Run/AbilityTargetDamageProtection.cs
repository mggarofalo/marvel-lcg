using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Reads a live constant source's explicit protection of another character.</summary>
internal static class AbilityTargetDamageProtection
{
    internal static bool Prevents(World world, AbilityProgram program, Card target,
        Func<Card, AbilityCondition, bool>? trace = null, IReadOnlySet<int>? excluded = null,
        AbilityQueryProjection? projection = null)
    {
        foreach (var owner in world.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)
                     && excluded?.Contains(card.ObjectId) != true))
        {
            var constants = AbilityProgramQueries.ActiveOn(world, program, owner)
                .Where(ability => ability.Trigger.Timing == AbilityType.Constant).ToArray();
            if (constants.Length == 0) continue;
            var bindings = new AbilityQueryContext(world, owner, new Occurrence(0, []),
                AbilityCardQueries.ControllerOf(world, owner), owner.Incarnation, null, null, null, []) { Projection = projection };
            var selectors = new AbilitySelectorEvaluation(bindings, null, program);
            var expressions = new AbilityExpressionEvaluation(new AbilityExpressionContext(bindings,
                ImmutableDictionary<string, long>.Empty, [], string.Empty, -1, false, null), selectors);
            bool Test(AbilityCondition condition) => trace?.Invoke(owner, condition) ?? expressions.Test(condition);
            if (constants.Any(ability => Protects(ability.Effect, target, selectors, Test))) return true;
        }
        return false;
    }

    private static bool Protects(AbilityEffect effect, Card target, AbilitySelectorEvaluation selectors,
        Func<AbilityCondition, bool> test) => effect switch
        {
            AbilityEffect.Sequence sequence => sequence.Effects.Any(child => Protects(child, target, selectors, test)),
            AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(child => Protects(child, target, selectors, test)),
            AbilityEffect.Conditional conditional => (test(conditional.Test) ? conditional.Then : conditional.Else)
                is { } branch && Protects(branch, target, selectors, test),
            AbilityEffect.PreventDamageWhile { Target: { } selected } protection =>
                selectors.Every(selected).Any(card => card.ObjectId == target.ObjectId) && test(protection.Condition),
            _ => false,
        };
}
