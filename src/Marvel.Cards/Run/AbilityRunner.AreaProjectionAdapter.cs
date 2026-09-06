using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    /// <summary>The top cards of a deck, in top-to-bottom order.</summary>
    private static IReadOnlyList<Card> TopCards(Area deck, int count) =>
        [.. deck.Cards.TakeLast(count).Reverse()];

    private static bool SingularAreaQueryIsStable(IReadOnlySet<DeckType> areas, Cast cast) =>
        AbilityAreaProjection.SingularAreaQueryIsStable(areas, AdmissionContext(cast));

    private static bool MayChangeAnyArea(
        AbilityEffect effect, IReadOnlySet<DeckType> queried, Cast cast,
        long multiplier = 1) =>
        AbilityAreaProjection.MayChangeAnyArea(effect, queried, AdmissionContext(cast), multiplier);

    private static bool EffectsMayChangeAnyArea(
        IReadOnlyList<AbilityEffect> effects, IReadOnlySet<DeckType> queried,
        Cast cast, long baseMultiplier = 1) =>
        AbilityAreaProjection.EffectsMayChangeAnyArea(
            effects, queried, AdmissionContext(cast), baseMultiplier);

    private static bool CostMayChangeAnyArea(
        AbilityCost cost, IReadOnlySet<DeckType> queried, Cast cast) =>
        AbilityAreaProjection.CostMayChangeAnyArea(cost, queried, AdmissionContext(cast));
}
