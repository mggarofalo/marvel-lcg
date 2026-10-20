using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Resolves an event's playable location without moving it or granting resource access.</summary>
public static class CardPlayOrigin
{
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
