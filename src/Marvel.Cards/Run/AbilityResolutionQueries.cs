using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// These adapters keep resolution state translation at the executor boundary while
// the shared query services remain independent of the mutable executor.
internal static class AbilityResolutionQueries
{
    internal static bool SingularAreaQueryIsStable(
        this AbilityResolutionExecution execution,
        IReadOnlySet<DeckType> areas,
        AbilityResolutionState cast) =>
        AbilityRuntimeQueries.SingularAreaQueryIsStable(areas, execution.AdmissionContext(cast));

    internal static bool MayChangeAnyArea(
        this AbilityResolutionExecution execution,
        AbilityEffect effect,
        IReadOnlySet<DeckType> queried,
        AbilityResolutionState cast,
        long multiplier = 1) =>
        AbilityRuntimeQueries.MayChangeAnyArea(
            effect, queried, execution.AdmissionContext(cast), multiplier);

    internal static bool EffectsMayChangeAnyArea(
        this AbilityResolutionExecution execution,
        IReadOnlyList<AbilityEffect> effects,
        IReadOnlySet<DeckType> queried,
        AbilityResolutionState cast,
        long baseMultiplier = 1) =>
        AbilityRuntimeQueries.EffectsMayChangeAnyArea(
            effects, queried, execution.AdmissionContext(cast), baseMultiplier);

    internal static bool CostMayChangeAnyArea(
        this AbilityResolutionExecution execution,
        AbilityCost cost,
        IReadOnlySet<DeckType> queried,
        AbilityResolutionState cast) =>
        AbilityRuntimeQueries.CostMayChangeAnyArea(
            cost, queried, execution.AdmissionContext(cast));

    internal static Card? Named(
        this AbilityResolutionExecution execution,
        AbilityCardBinding name,
        AbilityResolutionState cast) =>
        AbilityCardQueries.Named(name, cast.QueryContext());

    internal static int Resolver(
        this AbilityResolutionExecution execution,
        AbilityResolutionState cast) =>
        AbilityCardQueries.Resolver(cast.QueryContext());

    internal static Card ChosenPlayer(
        this AbilityResolutionExecution execution,
        AbilityResolutionState cast) =>
        AbilityCardQueries.ChosenPlayer(cast.QueryContext());

    internal static T EffectOf<T>(
        this AbilityResolutionExecution execution,
        AbilityEffect node,
        AbilityResolutionState cast)
        where T : AbilityEffect => (T)node;
}
