using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// The end of the player phase, and the deck running out.
/// </summary>
/// <remarks>
/// <para>
/// None of this is reachable by the recorded milestone game. It has one player
/// whose hand is full at every step, nothing that exhausts, and it ends in
/// three rounds — long before a forty-card deck could empty. So these boards
/// are built small enough to run out on purpose.
/// </para>
/// </remarks>
public sealed class PlayerPhaseEndTests
{
    [Rule("rr:end-of-player-phase.step.2")]
    [Rule("rr:hand-size")]
    [Fact]
    public void EachPlayerDrawsUpToTheirHandSize()
    {
        var printed = new Printed().With("identity", ("HS", "5"));
        var world = Board(printed, players: 2, deck: 10);
        var events = new List<GameEvent>();

        PhaseEnd.DrawToHandSize(world, printed, events);

        Assert.Equal(5, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(5, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:end-of-player-phase.step.2")]
    [Fact]
    public void AHandAtOrOverItsSizeDrawsNothing()
    {
        // "Draws **up to** their hand size", so this only ever adds cards.
        // Step 1 is where a hand comes down and it has already happened.
        var printed = new Printed().With("identity", ("HS", "2"));
        var world = Board(printed, players: 1, deck: 10);
        var events = new List<GameEvent>();
        Draw.Cards(world, 0, 4, "setup", events);

        PhaseEnd.DrawToHandSize(world, printed, events);

        Assert.Equal(4, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:end-of-player-phase.step.3")]
    [Fact]
    public void EveryCardInPlayReadiesIncludingTheEncounterCards()
    {
        // "Each player simultaneously readies all of their cards. **Ready each
        // exhausted encounter card.**" The second sentence is the one a
        // per-player loop would miss: an exhausted minion is nobody's card.
        var printed = new Printed();
        var world = Board(printed, players: 1, deck: 2);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var identity = world.Seats[0].IdentityCard;
        villain.Exhaust();
        identity.Exhaust();

        var events = new List<GameEvent>();
        PhaseEnd.ReadyCards(world, events);

        Assert.True(villain.Ready);
        Assert.True(identity.Ready);
        Assert.Equal(2, events.OfType<FieldSet>().Count(set => set.Field == "is_exhaust"));
    }

    [Rule("rr:exhausted")]
    [Fact]
    public void ACardOutOfPlayIsNotReadied()
    {
        // `rr:exhausted` is about cards in play. A card in a deck has no ready
        // state to return to, and emitting an event for one would put a field
        // on the wire that the card does not register.
        var printed = new Printed();
        var world = Board(printed, players: 1, deck: 2);
        var inDeck = world.Seats[0].Deck.Cards[0];
        inDeck.Exhaust();

        var events = new List<GameEvent>();
        PhaseEnd.ReadyCards(world, events);

        Assert.False(inDeck.Ready);
        Assert.Empty(events);
    }

    [Rule("rr:end-of-player-phase.step.1")]
    [Fact]
    public void APlayerMayDiscardNothing()
    {
        // "**May** discard any number of cards from their hand." A hand at or
        // under its size can be left alone.
        var printed = new Printed().With("identity", ("HS", "5"));
        var world = Board(printed, players: 1, deck: 10);
        var events = new List<GameEvent>();
        Draw.Cards(world, 0, 3, "setup", events);

        PhaseEnd.DiscardToHandSize(world, printed, 0, [], events);

        Assert.Equal(3, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:end-of-player-phase.step.1")]
    [Fact]
    public void AnOverFullHandMustComeDownAndTheEngineWillNotChoose()
    {
        // "**Must** discard down to their hand size if they have more cards
        // than their hand size." Which cards go is the player's decision --
        // they know what they are holding -- so an answer that leaves too many
        // is refused rather than topped up by the engine.
        var printed = new Printed().With("identity", ("HS", "2"));
        var world = Board(printed, players: 1, deck: 10);
        var events = new List<GameEvent>();
        Draw.Cards(world, 0, 5, "setup", events);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => PhaseEnd.DiscardToHandSize(world, printed, 0, [], events));

        Assert.Contains("must discard down to 2", thrown.Message, StringComparison.Ordinal);

        // And it is satisfied by discarding exactly the excess.
        var hand = world.Seats[0].Hand.Cards;
        PhaseEnd.DiscardToHandSize(
            world, printed, 0, [.. hand.Take(3).Select(card => card.ObjectId)], events);

        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:end-of-player-phase.step.1")]
    [Fact]
    public void ACardThatIsNotInThatHandCannotBeDiscardedFromIt()
    {
        var printed = new Printed().With("identity", ("HS", "5"));
        var world = Board(printed, players: 2, deck: 10);
        var events = new List<GameEvent>();
        Draw.Cards(world, 1, 1, "setup", events);
        int theirs = world.Seats[1].Hand.Cards[0].ObjectId;

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => PhaseEnd.DiscardToHandSize(world, printed, 0, [theirs], events));

        Assert.Contains("is not in p0's hand", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:player-deck.1")]
    [Fact]
    public void AnEmptyDeckIsRebuiltFromTheDiscardPileAndCostsAnEncounterCard()
    {
        // "If a player deck empties, the player shuffles their discard pile to
        // make a new deck. That player **immediately deals themself one
        // facedown encounter card** from the top of the encounter deck."
        var printed = new Printed().With("identity", ("HS", "9"));
        var world = Board(printed, players: 1, deck: 3);
        var events = new List<GameEvent>();

        Draw.Cards(world, 0, 3, "setup", events);
        foreach (var card in world.Seats[0].Hand.Cards.ToList())
        {
            Discard.Card(world, card, "setup", events);
        }

        // The deck emptied on the third draw and the discard was empty then, so
        // nothing reset. Discarding put a card back, and `rr:player-deck.4`
        // says the reset is owed and happens *then*.
        //
        // **One card, and the rule means one.** "The deck does not reset until
        // there is at least one card in the player's discard pile, then the
        // player deals themself one facedown encounter card." So the first
        // discard rebuilds a one-card deck and costs the encounter card; the
        // two discards after it land in an ordinary discard pile beside a deck
        // that is no longer empty. Reading it as "reset once the discard is
        // whole" would be inventing a condition the rule does not have -- and
        // the difference is a shuffle, which moves the random stream.
        Assert.Single(world.Seats[0].Deck.Cards);
        Assert.Equal(2, world.AreaOf(
            DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards.Count);
        Assert.Single(Queue(world, 0));
    }

    [Rule("rr:player-deck.2")]
    [Fact]
    public void DrawingContinuesAcrossTheReshuffle()
    {
        // "If the player's deck empties and reshuffles while the player was
        // drawing cards, the player continues to draw cards up to the specified
        // number." One card in the deck and three in the discard: asking for
        // three gets three.
        var printed = new Printed().With("identity", ("HS", "9"));
        var world = Board(printed, players: 1, deck: 4);
        var events = new List<GameEvent>();

        Draw.Cards(world, 0, 3, "setup", events);
        foreach (var card in world.Seats[0].Hand.Cards.ToList())
        {
            Discard.Card(world, card, "setup", events);
        }

        Draw.Cards(world, 0, 3, "a card", events);

        Assert.Equal(3, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:player-deck.4")]
    [Fact]
    public void ADeckAndADiscardBothEmptyIsALegalBoardAndNotAStall()
    {
        // "If a player deck empties and the player has no cards in their
        // discard pile, the deck does not reset until there is at least one
        // card in the player's discard pile." So there is simply no card to
        // draw, and asking for one is not a fault.
        var printed = new Printed().With("identity", ("HS", "9"));
        var world = Board(printed, players: 1, deck: 2);
        var events = new List<GameEvent>();

        Draw.Cards(world, 0, 2, "setup", events);
        Draw.Cards(world, 0, 5, "a card", events);

        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Empty(Queue(world, 0));
    }

    [Rule("rr:player-deck.1")]
    [Fact]
    public void TheResetHappensWhenTheDeckEmptiesRatherThanAtTheNextDraw()
    {
        // The trigger is the deck emptying, and the difference is observable:
        // the reshuffle draws from the game's one random stream, so moving it
        // one draw later changes every card drawn afterwards. Here it shows as
        // the encounter card arriving on the draw that empties the deck rather
        // than on the one after it.
        var printed = new Printed().With("identity", ("HS", "9"));
        var world = Board(printed, players: 1, deck: 2);
        var events = new List<GameEvent>();

        Draw.Cards(world, 0, 1, "setup", events);
        Discard.Card(world, world.Seats[0].Hand.Cards[0], "setup", events);
        Assert.Empty(Queue(world, 0));

        // One card left; drawing it empties the deck, and the discard has one.
        Draw.Cards(world, 0, 1, "a card", events);

        Assert.Single(Queue(world, 0));
        Assert.Single(world.Seats[0].Deck.Cards);
    }

    [Rule("rr:player-deck.1")]
    [Fact]
    public void TheRebuiltDeckIsShuffledFromTheGamesOneStream()
    {
        // "The player **shuffles** their discard pile to make a new deck." Not
        // "puts it back": a deck rebuilt in discard order would deal every
        // remaining card in a knowable sequence.
        //
        // Asserted two ways, because each catches what the other cannot. The
        // order changing says a shuffle happened; `WordsConsumed` says it came
        // out of the game's one MT19937 stream, which is the half that decides
        // whether a replay reproduces. A shuffle from a second generator would
        // reorder the deck perfectly and desynchronise every later draw.
        var printed = new Printed().With("identity", ("HS", "20"));
        var world = Board(printed, players: 1, deck: 10);
        var events = new List<GameEvent>();

        Draw.Cards(world, 0, 10, "setup", events);
        var discarded = new List<int>();
        foreach (var card in world.Seats[0].Hand.Cards.ToList())
        {
            discarded.Add(card.ObjectId);
            Discard.Card(world, card, "setup", events);
        }

        // The first discard rebuilt a *one-card* deck (`rr:player-deck.4`), and
        // a pile of one is not shuffled at all -- fewer than two cards draws
        // nothing, because calling through would consume a slot in the shared
        // stream for an outcome that cannot vary. So the nine discards after it
        // are what the next reset has to shuffle, and the draw that empties
        // that one-card deck is what triggers it.
        long before = world.Random.Generator.WordsConsumed;
        Draw.Cards(world, 0, 1, "a card", events);

        Assert.True(
            world.Random.Generator.WordsConsumed > before,
            "rebuilding a deck must draw from the game's one random stream");

        // The nine that were in the pile, in the order they were discarded, is
        // what the deck would hold if the cards had merely been moved back.
        var moved = discarded.Skip(1).ToList();
        var rebuilt = world.Seats[0].Deck.Cards.Select(card => card.ObjectId).ToList();

        Assert.Equal(moved.Count, rebuilt.Count);
        Assert.Equal([.. moved.Order()], [.. rebuilt.Order()]);
        Assert.NotEqual(moved, rebuilt);
    }

    private static IReadOnlyList<Card> Queue(World world, int seat) =>
        world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat)).Cards;

    /// <summary>A villain, one identity per seat, a small deck each.</summary>
    private static World Board(Printed printed, int players, int deck)
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard =
                world.CreateCard("identity", world.Seats[seat].Hero);
            for (int card = 0; card < deck; card++)
            {
                world.CreateCard($"p{seat}c{card}", world.Seats[seat].Deck);
            }
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        for (int card = 0; card < 8; card++)
        {
            world.CreateCard($"e{card}", world.AreaOf(DeckType.EncounterDeck));
        }

        return world;
    }

    /// <summary>Printed data for a handful of made-up cards.</summary>
    private sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal);

        public Printed With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found)
                ? found
                : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                table[key] = value;
            }

            return this;
        }

        public CardKind Kind(string faceId) => faceId switch
        {
            "identity" => CardKind.AlterEgo,
            "villain" => CardKind.EncounterVillain,
            _ => CardKind.Event,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number)
                ? number
                : fallback;
    }
}
