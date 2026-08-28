using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>The bounded discard operation used by encounter-card effects.</summary>
public sealed class EncounterDeckTests
{
    [Rule("rr:encounter-deck.2")]
    [Rule("rr:discard.4")]
    [Fact]
    public void DiscardUntilReturnsTheExactMatchAndLeavesTheCardBelowIt()
    {
        // "Until" stops on the matching discard. The card below it is the
        // control: consuming that card too would leave all the matched zones
        // plausible on their own.
        var facts = new Printed();
        var world = Board(facts);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var below = world.CreateCard("below", deck);
        var match = world.CreateCard("minion", deck);
        var above = world.CreateCard("above", deck);

        var found = EncounterDeck.DiscardUntil(
            world, facts, CardKind.Minion, "test", []);

        Assert.Same(match, found);
        Assert.Equal([below], deck.Cards);
        Assert.Equal(
            [above, match],
            world.AreaOf(DeckType.EncounterDiscardPile).Cards);
    }

    [Rule("rr:traits.1")]
    [Rule("rr:encounter-deck.2")]
    [Fact]
    public void OptionalTraitIsPartOfTheDiscardCondition()
    {
        // The first minion has the right kind and the wrong trait. The second
        // one is the exact object returned, and the card beneath it stays put.
        var facts = new Printed();
        var world = Board(facts);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var below = world.CreateCard("below", deck);
        var masters = world.CreateCard("masters", deck);
        var other = world.CreateCard("minion", deck);

        var found = EncounterDeck.DiscardUntil(
            world, facts, CardKind.Minion, "test", [], "MASTERS_OF_EVIL");

        Assert.Same(masters, found);
        Assert.Equal([below], deck.Cards);
        Assert.Equal(
            [other, masters],
            world.AreaOf(DeckType.EncounterDiscardPile).Cards);
    }

    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    [Fact]
    public void NoMatchResetsButDoesNotContinueIntoTheReplacementDeck()
    {
        // Emptying the current deck fulfills this discard effect. Its two
        // cards immediately form the replacement deck and neither is consumed
        // a second time, whichever order the seeded shuffle gives them.
        var facts = new Printed();
        var world = Board(facts);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var first = world.CreateCard("first", deck);
        var second = world.CreateCard("second", deck);
        var events = new List<GameEvent>();

        var found = EncounterDeck.DiscardUntil(
            world, facts, CardKind.Minion, "test", events);

        Assert.Null(found);
        Assert.Equal(2, deck.Cards.Count);
        Assert.Equal(
            [first.ObjectId, second.ObjectId],
            deck.Cards.Select(card => card.ObjectId).Order());
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Contains(events, happened => happened.Verb == "Reset");
        Assert.Equal(
            1,
            world.TheCardIn(DeckType.MainSchemesArea)!
                .Tokens[EncounterDeck.AccelerationToken]);
    }

    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    [Fact]
    public void ALastCardMatchSurvivesTheImmediateResetByIdentity()
    {
        // The matching card empties the deck. It is shuffled during the
        // immediate reset, but "that minion" is still the same card object and
        // the following effect can move it without drawing from the new top.
        var facts = new Printed();
        var world = Board(facts);
        var match = world.CreateCard("minion", world.AreaOf(DeckType.EncounterDeck));

        var found = EncounterDeck.DiscardUntil(
            world, facts, CardKind.Minion, "test", []);

        Assert.Same(match, found);
        Assert.Equal([match], world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
    }

    [Rule("rr:acceleration-token.2")]
    [Rule("rr:acceleration-token.2.1")]
    [Fact]
    public void ACardEffectUsesTheSameAccelerationStateAndEventAsAReset()
    {
        var facts = new Printed();
        var world = Board(facts);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var events = new List<GameEvent>();

        Assert.True(EncounterDeck.PlaceAccelerationToken(world, "card", events));

        Assert.Equal(1, scheme.Tokens[EncounterDeck.AccelerationToken]);
        var placed = Assert.Single(events.OfType<FieldSet>());
        Assert.Equal(scheme.ObjectId, placed.Card);
        Assert.Equal(EncounterDeck.AccelerationToken, placed.Field);
        Assert.Equal(0, placed.From);
        Assert.Equal(1, placed.To);
        Assert.Equal("card", placed.Trigger);
        Assert.Equal("Accelerate", placed.Verb);
    }

    [Rule("rr:encounter-deck.3")]
    [Rule("rr:acceleration-token.1")]
    [Fact]
    public void ADealFinishesAfterItsLastCardImmediatelyResetsTheEncounterDeck()
    {
        // "If the encounter deck empties during the resolution of any other
        // type of game effect," that effect "finishes resolving after the
        // encounter deck has been reset." The final deck card is still dealt,
        // while the discarded card becomes the replacement deck and the reset
        // places its required acceleration token before the deal finishes.
        var printed = new Printed();
        var world = Board(printed);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var card = world.CreateCard("card", deck);
        var replacement = world.CreateCard(
            "replacement", world.AreaOf(DeckType.EncounterDiscardPile));

        var events = new List<GameEvent>();
        var dealt = Deal.EncounterCard(world, 0, "test", events);

        Assert.Same(card, dealt);
        Assert.Same(
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)),
            card.Area);
        Assert.Equal([replacement], deck.Cards);
        int reset = events.FindIndex(item => item is CardsMoved moved
            && moved.Verb == "Reset");
        int deal = events.FindIndex(item => item is CardsMoved moved
            && moved.Verb == "Deal");
        Assert.True(reset >= 0);
        Assert.True(deal > reset);
        Assert.Equal(
            1,
            world.TheCardIn(DeckType.MainSchemesArea)!
                .Tokens[EncounterDeck.AccelerationToken]);
    }

    [Rule("rr:encounter-deck.4")]
    [Fact]
    public void TakingTheFinalCardWithNoDiscardPileEndsTheGame()
    {
        // If there are "no cards in both the encounter deck and the encounter
        // discard pile simultaneously," the resulting infinite reset loop
        // means "the players lose." Taking the only card creates that boundary.
        var printed = new Printed();
        var world = Board(printed);
        world.CreateCard("card", world.AreaOf(DeckType.EncounterDeck));

        Deal.EncounterCard(world, 0, "test", []);

        Assert.Equal(Outcome.PlayersLose, world.Result);
    }

    [Rule("rr:encounter-deck.2")]
    [Fact]
    public void DiscardingTheFinalCardResetsItWithoutContinuingIntoTheNewDeck()
    {
        // A specified-number discard stops when "the encounter deck is empty."
        // The discarded card enters the pile first, so the immediate reset can
        // rebuild from it without the empty-pair loss or discarding it twice.
        var printed = new Printed();
        var world = Board(printed);
        var card = world.CreateCard("card", world.AreaOf(DeckType.EncounterDeck));

        var discarded = EncounterDeck.DiscardTop(world, 5, "test", []);

        Assert.Equal([card], discarded);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Equal([card], world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Empty(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Equal(
            1,
            world.TheCardIn(DeckType.MainSchemesArea)!
                .Tokens[EncounterDeck.AccelerationToken]);
    }

    private static World Board(ICardFacts facts)
    {
        var world = new World(facts, players: 1);
        world.CreateSeat("player");
        world.Seats[0].IdentityCard = world.CreateCard("hero", world.Seats[0].Hero);
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "scheme" => CardKind.MainScheme,
            "minion" or "masters" => CardKind.Minion,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string faceId) =>
            faceId == "masters" ? ["MASTERS_OF_EVIL"] : [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>();

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
