using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Resolves playable locations without moving cards or granting resource access.</summary>
public static class CardPlayOrigin
{
    /// <summary>Enumerates permitted ordinary-play origins in stable physical-card order.</summary>
    internal static IEnumerable<Card> Candidates(World world, int player) =>
        world.Seats[player].Hand.Cards.Concat(world.Areas
            .Where(area => area.Type == DeckType.DiscardPile && area.PlayArea == PlayArea.Of(player))
            .SelectMany(area => area.Cards))
            .Where(card => CanPlay(world, card, player))
            .OrderBy(card => card.ObjectId);

    /// <summary>Checks ordinary hand access and bounded passive discard permission.</summary>
    public static bool CanPlay(World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        if (player < 0 || player >= world.Seats.Count) return false;
        return card.Area == world.Seats[player].Hand || CanPlayEvent(world, card, player)
            || card.Owner == player && card.Area.Type == DeckType.DiscardPile && card.Area.PlayArea == PlayArea.Of(player)
                && EffectiveCards.Kind(card, world.Facts) == CardKind.Ally
                && world.CardPlayAbilities.CanPlayFromDiscard(world, card, player);
    }

    /// <summary>Checks hand access or an explicit live card-text permission for a stored event.</summary>
    public static bool CanPlayEvent(World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        if (player < 0 || player >= world.Seats.Count || card.Owner != player
            || EffectiveCards.Kind(card, world.Facts) != CardKind.Event) return false;
        // rr:player-turn.5 permits an event Action from hand. Printed 04003
        // additionally permits playing attached ARROW events "as if they were in your hand".
        return card.Area == world.Seats[player].Hand
            || BeneathCards.IsStored(card.Area.Type) && world.CardPlayAbilities.CanPlayFromStorage(world, card, player);
    }
}
