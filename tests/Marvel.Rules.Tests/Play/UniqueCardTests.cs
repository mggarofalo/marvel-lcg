using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class UniqueCardTests
{
    [Rule("rr:unique-icon.1.1")]
    [Fact]
    public void UniqueCardsWithTheSameBareTitleMatch()
    {
        // Two unique cards match when they “share a title, and both have no
        // subtitle and no alter-ego title.”
        var facts = Cards().Card("one", CardKind.Support, "Jarnbjorn", unique: true)
                           .Card("two", CardKind.Upgrade, "Jarnbjorn", unique: true);

        Assert.True(Uniqueness.Matches(facts, ["one"], ["two"]));
    }

    [Rule("rr:unique-icon.1.2")]
    [Fact]
    public void ASubtitleMatchesAnotherCardsTitle()
    {
        var facts = Cards().Card("ally", CardKind.Ally, "Black Panther", "T'Challa", true)
                           .Card("other", CardKind.Ally, "T'Challa", unique: true);

        Assert.True(Uniqueness.Matches(facts, ["ally"], ["other"]));
    }

    [Rule("rr:unique-icon.4.1")]
    [Fact]
    public void AMatchingUniqueAllyPutIntoPlayHasNoEffect()
    {
        // A matching player card “cannot be played or put into play. Any
        // effect that attempts to do so has no effect.”
        var facts = Cards().Card("identity", CardKind.AlterEgo, "Hero")
                           .Card("present", CardKind.Ally, "Person", unique: true)
                           .Card("waiting", CardKind.Ally, "Person", unique: true);
        var world = Board(facts);
        world.CreateCard("present", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        var waiting = world.CreateCard("waiting", world.Seats[0].Deck);

        CardPlay.PutAllyIntoPlay(world, facts, new NoCardAbilities(), waiting, 0, "test", []);

        Assert.Same(world.Seats[0].Deck, waiting.Area);
    }

    [Rule("rr:unique-icon.4.1")]
    [Fact]
    public void AMatchingUniquePlayerCardCannotBePlayed()
    {
        var facts = Cards().Card("identity", CardKind.AlterEgo, "Hero")
                           .Card("present", CardKind.Support, "Place", unique: true)
                           .Card("waiting", CardKind.Support, "Place", unique: true);
        var world = Board(facts);
        world.CreateCard("present", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        var waiting = world.CreateCard("waiting", world.Seats[0].Hand);

        Assert.Null(CardPlay.Price(world, facts, world.Seats[0], waiting));
    }

    [Rule("rr:unique-icon.4.1")]
    [Fact]
    public void MatchingUniqueCardsInDifferentGameAreasDoNotBlockEachOther()
    {
        // pack:mc11:rules-clarifications: “Cards in one game area cannot
        // affect other game areas, so a unique card in one game area places no
        // limitations on the others.”
        var facts = Cards().Card("identity", CardKind.AlterEgo, "Hero")
                           .Card("present", CardKind.Ally, "Person", unique: true)
                           .Card("waiting", CardKind.Ally, "Person", unique: true);
        var world = SplitBoard(facts);
        world.CreateCard("present", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        var waiting = world.CreateCard("waiting", world.Seats[1].Deck);

        CardPlay.PutAllyIntoPlay(world, facts, new NoCardAbilities(), waiting, 1, "test", []);

        Assert.Equal(DeckType.AlliesArea, waiting.Area.Type);
        Assert.Equal(PlayArea.Of(1), waiting.Area.PlayArea);
    }

    [Rule("rr:unique-icon.4.1")]
    [Fact]
    public void CrossControllerAllyIsBlockedInItsDestinationGameArea()
    {
        // A matching player card “cannot be played or put into play.” The
        // relevant game area is the controller's destination, even when an
        // effect puts another player's ally there.
        var facts = Cards().Card("identity", CardKind.AlterEgo, "Hero")
                           .Card("present", CardKind.Ally, "Person", unique: true)
                           .Card("waiting", CardKind.Ally, "Person", unique: true);
        var world = SplitBoard(facts);
        world.CreateCard("present", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)));
        var waiting = world.CreateCard("waiting", world.Seats[0].Deck);

        CardPlay.PutAllyIntoPlay(world, facts, new NoCardAbilities(), waiting, 1, "test", []);

        Assert.Same(world.Seats[0].Deck, waiting.Area);
    }

    [Rule("rr:unique-icon.4.1")]
    [Fact]
    public void CrossControllerAllyIgnoresMatchingCardInAnotherGameArea()
    {
        // pack:mc11:rules-clarifications: “Cards in one game area cannot
        // affect other game areas.” A matching card in the owner's area does
        // not block the ally from entering another controller's area.
        var facts = Cards().Card("identity", CardKind.AlterEgo, "Hero")
                           .Card("present", CardKind.Ally, "Person", unique: true)
                           .Card("waiting", CardKind.Ally, "Person", unique: true);
        var world = SplitBoard(facts);
        world.CreateCard("present", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        var waiting = world.CreateCard("waiting", world.Seats[0].Deck);

        CardPlay.PutAllyIntoPlay(world, facts, new NoCardAbilities(), waiting, 1, "test", []);

        Assert.Equal(DeckType.AlliesArea, waiting.Area.Type);
        Assert.Equal(PlayArea.Of(1), waiting.Area.PlayArea);
        Assert.Equal(0, waiting.Owner);
    }

    [Rule("rr:unique-icon.4.2")]
    [Fact]
    public void ARevealedMatchingEncounterCardIsDiscardedAndReplacedFacedown()
    {
        // The matching encounter card is discarded, its reveal is ignored,
        // and “the player revealing it is dealt a facedown encounter card.”
        var facts = Cards().Card("identity", CardKind.AlterEgo, "Hero")
                           .Card("present", CardKind.Ally, "Person", unique: true)
                           .Card("minion", CardKind.Minion, "Person", unique: true)
                           .Card("replacement", CardKind.Treachery, "Replacement")
                           .Card("reserve", CardKind.Treachery, "Reserve");
        var world = Board(facts);
        world.CreateCard("present", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        world.CreateCard("reserve", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("replacement", world.AreaOf(DeckType.EncounterDeck));
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.RevealingArea));

        Reveal.Resolve(world, facts, minion, 0, [], new Occurrence(1, ["CardRevealed"]));

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Rule("rr:unique-icon.2.1")]
    [Fact]
    public void SetupEndingDoesNotPreventMatchingUniqueCardsFromEnteringADeck()
    {
        // “If a player is required to add a unique card to their deck after
        // setup has begun, the player can add that card even if it would cause
        // the player to have multiple matching cards in their deck.”
        var facts = Cards().Card("identity", CardKind.AlterEgo, "Hero")
                           .Card("first", CardKind.Ally, "Person", unique: true)
                           .Card("second", CardKind.Ally, "Person", unique: true);
        var world = Board(facts);
        world.CreateCard("first", world.Seats[0].Deck);
        var second = world.CreateCard(
            "second", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));

        World.MoveToTop(second, world.Seats[0].Deck);

        Assert.Equal(2, world.Seats[0].Deck.Cards.Count);
    }

    private static World Board(Facts facts)
    {
        var world = new World(facts, 1);
        world.CreateSeat("p0");
        world.Seats[0].IdentityCard = world.CreateCard("identity", world.Seats[0].Hero);
        return world;
    }

    private static World SplitBoard(Facts facts)
    {
        var world = new World(facts, 2);
        for (int player = 0; player < 2; player++)
        {
            world.CreateSeat($"p{player}");
            world.Seats[player].IdentityCard =
                world.CreateCard("identity", world.Seats[player].Hero);
            var ownArea = world.CreateGameArea();
            world.Join(PlayArea.Of(player), ownArea, "test", []);
        }
        return world;
    }

    private static Facts Cards() => new();

    private sealed class Facts : ICardFacts
    {
        private readonly Dictionary<string, (CardKind Kind, string Title, string Subtitle)> cards = [];
        private readonly Dictionary<string, Dictionary<string, string>> attributes = [];

        public Facts Card(
            string id, CardKind kind, string title, string subtitle = "", bool unique = false)
        {
            cards[id] = (kind, title, subtitle);
            attributes[id] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Cost"] = "0",
            };
            if (unique)
            {
                attributes[id]["Unique"] = "1";
            }
            return this;
        }

        public CardKind Kind(string faceId) => cards[faceId].Kind;
        public string Title(string faceId) => cards[faceId].Title;
        public string Subtitle(string faceId) => cards[faceId].Subtitle;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => attributes[faceId];
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
