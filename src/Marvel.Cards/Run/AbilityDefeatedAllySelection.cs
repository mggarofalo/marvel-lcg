using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// The selector spelling is an engine choice. Printed 08031 requires both former
// control and the exact physical card's present membership in your discard pile.
internal static class AbilityDefeatedAllySelection
{
    internal static IReadOnlyList<Card> Every(AbilityQueryContext context) =>
        Find(context) is { } ally ? [ally] : [];

    internal static Card? Find(AbilityQueryContext context)
    {
        int player = context.Player;
        if (context.Defeat is not { Kind: CardKind.Ally } defeat
            || defeat.Controller != player) return null;
        var card = context.World.Cards[defeat.Card];
        // rr:leaves-play.1: "a new copy of the card". A reentered copy cannot
        // borrow this defeat. Out-of-play movement alone does not create a copy.
        return card.Incarnation == defeat.Incarnation
            && card.Owner == player && card.Area.Type == DeckType.DiscardPile
            && card.Area.PlayArea == PlayArea.Of(player) ? card : null;
    }
}
