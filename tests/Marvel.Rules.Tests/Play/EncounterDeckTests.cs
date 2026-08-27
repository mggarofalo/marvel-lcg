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
