using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    private static bool InspectsConcealedPile(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.InAreas areas => areas.Areas.Any(area => area is
            AbilitySearchArea.YourDeck or AbilitySearchArea.EncounterDeck),
        AbilityCardSelection.WithTrait filtered => InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.Discardable filtered => InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.Ranked ranked => InspectsConcealedPile(ranked.Cards),
        _ => false,
    };

    private AbilitySingularAreaAdmission? SingularAreaAdmission(AbilityResolutionState cast) =>
        cast.Reachability.CheckingInitiation
            ? areas => SingularAreaQueryIsStable(areas, cast)
            : null;

    private Card? Find(AbilityCardSelection selector, AbilityResolutionState cast) =>
        new AbilitySelectorEvaluation(
            cast.QueryContext(), SingularAreaAdmission(cast), program).Find(selector);

    private IReadOnlyList<Card> Every(AbilityCardSelection selector, AbilityResolutionState cast) =>
        new AbilitySelectorEvaluation(cast.QueryContext(), null, program).Every(selector);

    private static bool CanRemoveByEffect(AbilityCardSelection selector, AbilityResolutionState cast, Card target) =>
        new AbilitySelectorEvaluation(cast.QueryContext()).CanRemove(selector, target);

    private static Area Area(AbilitySearchArea area, AbilityResolutionState cast) => area switch
    {
        AbilitySearchArea.EncounterDeck => cast.World.AreaOf(DeckType.EncounterDeck),
        AbilitySearchArea.EncounterDiscardPile => cast.World.AreaOf(DeckType.EncounterDiscardPile),
        AbilitySearchArea.ScenarioSetAside => cast.World.AreaOf(DeckType.AsideDeck),
        AbilitySearchArea.YourDeck => cast.World.Seats[cast.Player].Deck,
        _ => throw new InvalidOperationException("Unknown compiled search area"),
    };

    private static bool ContainsYouOrYour(AbilityCardSelection selector) =>
        AbilityPlayerBindingAnalysis.Contains(selector);
}
