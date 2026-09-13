namespace Marvel.Rules.State;

/// <summary>Authoritative ownership and control queries for one card.</summary>
/// <remarks>
/// <see cref="Card.Owner"/> remains an ownership fact. A player card in a
/// player's in-play area is instead controlled by that area's player, which is
/// how a changed-control card remains owned by one player and controlled by
/// another. Scenario cards and every out-of-play card fall back to ownership;
/// placement in a player's set-aside or tucked area never grants control.
/// </remarks>
public static class CardControl
{
    /// <summary>Whether the card follows player-card control semantics.</summary>
    public static bool IsPlayerCard(ICardFacts facts, Card card)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        CardKind kind = facts.Kind(card.FaceId);
        return kind is CardKind.AlterEgo
            or CardKind.Hero
            or CardKind.Ally
            or CardKind.Event
            or CardKind.Resource
            or CardKind.Support
            or CardKind.Upgrade
            || kind == CardKind.Unknown && card.Owner != World.Scenario;
    }

    /// <summary>Returns the card's current controller, falling back to its owner out of play.</summary>
    /// <remarks>
    /// <c>rr:ownership-and-control.5</c> moves a changed-control player card to
    /// its controller's play area. Ownership remains on <see cref="Card.Owner"/>,
    /// so a consumer must not use either field for both facts.
    /// </remarks>
    public static int ControllerOf(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        return IsPlayerCard(world.Facts, card)
            && DeckTypes.IsInPlay(card.Area.Type)
            && card.Area.PlayArea.IsPlayers
                ? card.Area.PlayArea.Player
                : card.Owner;
    }
}
