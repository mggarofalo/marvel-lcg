using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Retains the exact pre-arrow scheme when payment outlives that copy.</summary>
internal static class AbilitySchemeCostBinding
{
    internal static bool HasDeparted(AbilityProgram program, string face,
        AbilityEffect node, AbilityQueryContext bindings)
    {
        if (bindings.ChosenBinding is not { } chosen) return false;
        if (chosen.Card.Area.Id == chosen.Area && chosen.Card.Incarnation == chosen.Incarnation)
            return false;
        return program.On(face).Any(ability => ability.Cost is AbilityCost.ChooseScheme
            && Contains(ability.Effect, node));
    }

    private static bool Contains(AbilityEffect root, AbilityEffect node) =>
        ReferenceEquals(root, node)
        || AbilityEffectStructure.AllEffectChildren(root).Any(child => Contains(child, node));
}
