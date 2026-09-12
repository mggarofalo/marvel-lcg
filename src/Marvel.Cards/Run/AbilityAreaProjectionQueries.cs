using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityRuntimeQueries;
using static Marvel.Cards.Run.AbilityProjectedStateQueries;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Projects how a compiled ability can change queried card areas.</summary>
internal static class AbilityAreaProjectionQueries
{
    internal static bool EffectsMayChangeAnyArea(
        IReadOnlyList<AbilityEffect> effects, IReadOnlySet<DeckType> queried,
        AbilityAdmissionContext context, long baseMultiplier = 1)
        => ProjectedAreaMayChange(effects, null, queried, context, baseMultiplier);

    internal static bool CostMayChangeAnyArea(
        AbilityCost cost, IReadOnlySet<DeckType> queried, AbilityAdmissionContext context)
        => ProjectedAreaMayChange([], cost, queried, context);

    private static bool ProjectedAreaMayChange(
        IReadOnlyList<AbilityEffect> effects, AbilityCost? cost,
        IReadOnlySet<DeckType> queried, AbilityAdmissionContext context, long baseMultiplier = 1) =>
        new AbilityAreaProjection(effects, cost, queried, context, baseMultiplier).MayChange();

}
