using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Puts a physical player card into play as an explicitly defined minion.</summary>
public static class FacedownMinions
{
    /// <summary>Engages the top player card, retaining ownership and hiding its printed text.</summary>
    public static Card? EngageTop(
        World world, int player, EffectiveCardProfile profile, string trigger,
        string verb, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(verb);
        if (profile.Kind != CardKind.Minion)
            throw new ArgumentException("Engagement requires a minion profile", nameof(profile));
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, world.Seats.Count);
        PlayerDeck.Reset(world, player, events);
        var deck = world.Seats[player].Deck;
        if (deck.Cards.Count == 0) return null;
        var card = deck.Cards[^1];
        var from = card.Area;
        var engaged = world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(player), cardOwner: World.Scenario);
        World.MoveToTop(card, engaged);
        card.AssignProfile(profile);
        card.TurnFaceDown();
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(engaged),
            [new Landing(card.ObjectId, engaged.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = verb,
            Subjects = new Dictionary<int, string> { [card.ObjectId] = profile.Title },
        });
        PlayerDeck.Reset(world, player, events);
        return card;
    }
}
