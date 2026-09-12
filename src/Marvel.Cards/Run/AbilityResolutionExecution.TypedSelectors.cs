using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionTypedSelectors
{
    internal static bool InspectsConcealedPile(this AbilityResolutionExecution execution, AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.InAreas areas => areas.Areas.Any(area => area is
            AbilitySearchArea.YourDeck or AbilitySearchArea.EncounterDeck),
        AbilityCardSelection.WithTrait filtered => execution.InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => execution.InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.Discardable filtered => execution.InspectsConcealedPile(filtered.Cards),
        AbilityCardSelection.Ranked ranked => execution.InspectsConcealedPile(ranked.Cards),
        _ => false,
    };

    internal static AbilitySingularAreaAdmission? SingularAreaAdmission(this AbilityResolutionExecution execution, AbilityResolutionState cast) =>
        cast.Reachability.CheckingInitiation
            ? areas => execution.SingularAreaQueryIsStable(areas, cast)
            : null;

    internal static Card? Find(this AbilityResolutionExecution execution, AbilityCardSelection selector, AbilityResolutionState cast) =>
        new AbilitySelectorEvaluation(
            cast.QueryContext(), execution.SingularAreaAdmission(cast), execution.program).Find(selector);

    internal static IReadOnlyList<Card> Every(this AbilityResolutionExecution execution, AbilityCardSelection selector, AbilityResolutionState cast) =>
        new AbilitySelectorEvaluation(cast.QueryContext(), null, execution.program).Every(selector);

    internal static bool CanRemoveByEffect(this AbilityResolutionExecution execution, AbilityCardSelection selector, AbilityResolutionState cast, Card target) =>
        new AbilitySelectorEvaluation(cast.QueryContext()).CanRemove(selector, target);

    internal static Area Area(this AbilityResolutionExecution execution, AbilitySearchArea area, AbilityResolutionState cast) => area switch
    {
        AbilitySearchArea.EncounterDeck => cast.World.AreaOf(DeckType.EncounterDeck),
        AbilitySearchArea.EncounterDiscardPile => cast.World.AreaOf(DeckType.EncounterDiscardPile),
        AbilitySearchArea.ScenarioSetAside => cast.World.AreaOf(DeckType.AsideDeck),
        AbilitySearchArea.YourDeck => cast.World.Seats[cast.Player].Deck,
        _ => throw new InvalidOperationException("Unknown compiled search area"),
    };

    internal static bool ContainsYouOrYour(this AbilityResolutionExecution execution, AbilityCardSelection selector) =>
        AbilityPlayerBindingAnalysis.Contains(selector);
}
