using Marvel.Rules.Events;
using Marvel.Rules.Play;

namespace Marvel.Rules.State;

/// <summary>Player cards that an Ultron effect puts into play as facedown Drone minions.</summary>
/// <remarks>
/// <para>
/// The runtime identity is derived from state the digest already records: a
/// player-owned card is facedown in an engagement area. Its printed
/// <see cref="Card.FaceId"/> remains the player card underneath it. This keeps
/// both object identity and the state-digest v2 wire shape unchanged.
/// </para>
/// <para>
/// Ultron Drones (<c>01140</c>) supplies the blank minion's base SCH, ATK and
/// hit points. The effect that creates one supplies its card type and DRONE
/// trait. None of the facedown player's printed attributes or traits are active.
/// </para>
/// </remarks>
public static class FacedownDrones
{
    /// <summary>The trait given to every facedown Drone minion.</summary>
    public const string Trait = "DRONE";

    private static readonly IReadOnlyList<string> DroneTraits = [Trait];

    /// <summary>Whether a card currently has facedown-Drone semantics.</summary>
    public static bool Is(Card card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Owner >= 0
            && !card.FaceUp
            && card.Area.Type == DeckType.EngagedEnemiesArea;
    }

    /// <summary>The card's current type, accounting for a facedown Drone.</summary>
    public static CardKind Kind(Card card, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(facts);
        return Is(card) ? CardKind.Minion : facts.Kind(card.FaceId);
    }

    /// <summary>The card's current base value before modifiers.</summary>
    /// <remarks>
    /// <c>rr:base-value</c> defines this as the value before modifiers. For a
    /// facedown Drone, <c>01140</c> defines SCH, ATK and hit points as 1; every
    /// other printed attribute is blank while the player card is facedown.
    /// </remarks>
    public static long BaseValue(
        Card card, ICardFacts facts, string attribute, int players, long fallback = 0)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(attribute);

        if (!Is(card))
        {
            return facts.PrintedValue(card.FaceId, attribute, players, fallback);
        }

        return attribute is "SCH" or "ATK" or "HP" ? 1 : fallback;
    }

    /// <summary>The card's active inherent traits before granted traits.</summary>
    public static IReadOnlyList<string> InherentTraits(Card card, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(facts);
        return Is(card) ? DroneTraits : facts.Traits(card.FaceId);
    }

    /// <summary>
    /// Puts the top card of a player's deck into play facedown, engaged with
    /// that player as a Drone minion.
    /// </summary>
    /// <returns>The Drone, or <c>null</c> if the deck and discard pile are empty.</returns>
    public static Card? EngageTop(
        World world, int player, string trigger, string verb, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(verb);
        ArgumentNullException.ThrowIfNull(events);

        if (player < 0 || player >= world.Seats.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(player));
        }

        PlayerDeck.Reset(world, player, events);
        var deck = world.Seats[player].Deck;
        if (deck.Cards.Count == 0)
        {
            return null;
        }

        var card = deck.Cards[^1];
        var from = card.Area;
        var engaged = world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(player), cardOwner: World.Scenario);
        World.MoveToTop(card, engaged);
        card.TurnFaceDown();
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(engaged),
            [new Landing(card.ObjectId, engaged.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = verb,
        });

        // `rr:player-deck.1` triggers when the deck empties, not when the
        // player next tries to draw. Moving its top card can empty it too.
        PlayerDeck.Reset(world, player, events);
        return card;
    }

    /// <summary>All facedown Drones in play, in object-id order.</summary>
    public static IReadOnlyList<Card> InPlay(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return [.. world.Cards.Where(Is)];
    }

    /// <summary>A player's engaged facedown Drones, in object-id order.</summary>
    public static IReadOnlyList<Card> EngagedWith(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        return
        [
            .. world.Cards.Where(card =>
                Is(card) && card.Area.PlayArea == PlayArea.Of(player)),
        ];
    }
}
