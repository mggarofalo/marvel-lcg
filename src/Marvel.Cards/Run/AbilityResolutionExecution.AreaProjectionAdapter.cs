using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    private bool SingularAreaQueryIsStable(IReadOnlySet<DeckType> areas, AbilityResolutionState cast) =>
        AbilityRuntimeQueries.SingularAreaQueryIsStable(areas, AdmissionContext(cast));

    private bool MayChangeAnyArea(
        AbilityEffect effect, IReadOnlySet<DeckType> queried, AbilityResolutionState cast,
        long multiplier = 1) =>
        AbilityRuntimeQueries.MayChangeAnyArea(effect, queried, AdmissionContext(cast), multiplier);

    private bool EffectsMayChangeAnyArea(
        IReadOnlyList<AbilityEffect> effects, IReadOnlySet<DeckType> queried,
        AbilityResolutionState cast, long baseMultiplier = 1) =>
        AbilityRuntimeQueries.EffectsMayChangeAnyArea(
            effects, queried, AdmissionContext(cast), baseMultiplier);

    private bool CostMayChangeAnyArea(
        AbilityCost cost, IReadOnlySet<DeckType> queried, AbilityResolutionState cast) =>
        AbilityRuntimeQueries.CostMayChangeAnyArea(cost, queried, AdmissionContext(cast));
}
