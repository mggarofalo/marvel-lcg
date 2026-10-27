using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// The query spelling and physical-id order are engine choices.
internal static class AbilityInPlayCardQuery
{
    internal static void CollectAreas(AbilityCardSelection selector, HashSet<DeckType> areas)
    {
        if (selector is AbilityCardSelection.StatusCardsOn) areas.Add(DeckType.StatusArea);
        if (selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.CardsInPlay })
            areas.UnionWith(Enum.GetValues<DeckType>().Where(DeckTypes.IsInPlay));
    }

    internal static void ValidateProjection(AbilityCardSelection selector, AreaProjectionState state)
    {
        if (selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.CardsInPlay }
            && (state.Entered.Count > 0 || state.Departed.Count > 0 || state.EngagedWith.Count > 0))
            throw new RulesNotImplementedException(
                "cardsInPlay after projected movement requires complete projected placement");
    }

    internal static IReadOnlyList<Card> Live(AbilityQueryContext context) =>
        // rr:in-play-and-out-of-play.4: abilities "can only target, cards that
        // are in play". Areas own membership, including hosted upgrades.
        // Places owns the published game-area partitions and central exception.
        [.. context.World.Areas.Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => Places.CanAffect(context.World, context.Source, card))
            .OrderBy(card => card.ObjectId)];
}
