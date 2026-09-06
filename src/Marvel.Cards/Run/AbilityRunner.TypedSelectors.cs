using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
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

    private static AbilitySingularAreaAdmission? SingularAreaAdmission(Cast cast) =>
        cast.Reachability.CheckingInitiation
            ? areas => SingularAreaQueryIsStable(areas, cast)
            : null;

    private static Card? Find(AbilityCardSelection selector, Cast cast) =>
        new AbilitySelectorEvaluation(cast.QueryContext(), SingularAreaAdmission(cast)).Find(selector);

    private static IReadOnlyList<Card> Every(AbilityCardSelection selector, Cast cast) =>
        new AbilitySelectorEvaluation(cast.QueryContext()).Every(selector);

    private static bool CanRemoveByEffect(AbilityCardSelection selector, Cast cast, Card target) =>
        new AbilitySelectorEvaluation(cast.QueryContext()).CanRemove(selector, target);

    private static Area Area(AbilitySearchArea area, Cast cast) => area switch
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
