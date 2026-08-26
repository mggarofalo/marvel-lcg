using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// Paying for a card and putting it into play.
/// </summary>
/// <remarks>
/// The recorded milestone game offers four cards to play and plays none of
/// them, because its sampling policy declines everything. So the offer is held
/// against the recording in <c>PlayerPhaseTests</c> and what happens when one
/// is taken is held here.
/// </remarks>
public sealed class CardPlayTests
{
    [Rule("rr:resource.3")]
    [Theory]
    // "A number of resources equal to (or greater than) the card's cost must be
    // generated. For most cards, any type (or mix of types) of resources can be
    // used to pay this cost."
    [InlineData("", 0, true)]
    [InlineData("B", 1, true)]
    [InlineData("B", 2, false)]
    [InlineData("RYB", 3, true)]
    // `rr:cost.4` permits generating beyond the cost, so more is not a failure.
    [InlineData("RYBG", 2, true)]
    public void ACostIsACountOfResourcesOfAnyType(string generated, int cost, bool pays)
    {
        Assert.Equal(pays, Resources.Pays(generated, cost));
    }

    [Rule("rr:resource.2")]
    [Rule("rr:resource.4")]
    [Theory]
    // "Many abilities require specific resource types, and the specified types
    // in the specified quantities must be generated."
    [InlineData("BB", "B", true)]
    [InlineData("RR", "B", false)]
    // "Wild resources can be used as their type or any of the other types."
    [InlineData("GG", "B", true)]
    [InlineData("BG", "BB", true)]
    [InlineData("BR", "BB", false)]
    public void AWildResourceStandsInForAnyType(string generated, string required, bool pays)
    {
        Assert.Equal(pays, Resources.Pays(generated, required.Length, required));
    }

    [Rule("rr:resource.2")]
    [Fact]
    public void AnExactMatchIsSpentBeforeAWild()
    {
        // One mental and one wild against a requirement of one mental and one
        // physical. Spending the wild on the mental leaves nothing for the
        // physical; spending the mental leaves the wild to cover it.
        Assert.True(Resources.Pays("BG", 2, "BR"));
    }

    [Rule("rr:cost.2")]
    [Fact]
    public void ACostThatIsNotANumberSaysSoRatherThanReadingAsZero()
    {
        // A cost of `X` (`rr:initiating-abilities.step.3`, the player chooses
        // the value) and the per-player icon (`rr:cost.2`) are both printed in
        // this field. Reading either as zero would make the card free.
        var printed = new Printed().With("odd", ("Cost", "X"));

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => Resources.Cost("odd", printed));

        Assert.Contains("is not a number", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:play-put-into-play")]
    [Fact]
    public void AnAllyEntersPlayAndAnUpgradeAttaches()
    {
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var ally = InHand(world, "ally");
        var events = new List<GameEvent>();

        CardPlay.Play(world, printed, new Silent(), seat, ally, [Pay(world, "res")], events);

        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);

        var upgrade = InHand(world, "upgrade");
        CardPlay.Play(world, printed, new Silent(), seat, upgrade, [Pay(world, "res")], events);

        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
        Assert.Equal(seat.IdentityCard.ObjectId, upgrade.Area.Host);
        Assert.Contains(events.OfType<CardAttached>(), e => e.Card == upgrade.ObjectId);
    }

    [Rule("rr:play-put-into-play.2")]
    [Fact]
    public void AnEventResolvesAndGoesToTheDiscardPile()
    {
        // "When an event card is played, place it on the table, resolve its
        // ability, and place the card in its owner's discard pile."
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var card = InHand(world, "event");
        var abilities = new Counting();

        CardPlay.Play(world, printed, abilities, seat, card, [], []);

        Assert.Equal(1, abilities.Resolved);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }

    [Rule("rr:cost.3")]
    [Fact]
    public void PayingDiscardsTheCardsSpent()
    {
        // "A player spends resources that they generate by discarding cards
        // from their hand."
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var ally = InHand(world, "ally");
        var spent = world.Cards[Pay(world, "res")];

        CardPlay.Play(world, printed, new Silent(), seat, ally, [spent.ObjectId], []);

        Assert.Equal(DeckType.DiscardPile, spent.Area.Type);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnUnderpaymentAbortsWithoutPayingAnything()
    {
        // "If this step is reached and the cost(s) cannot be paid, **abort this
        // process without paying any costs.**" So a payment one short discards
        // nothing at all -- not the cards it did cover.
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var expensive = InHand(world, "expensive");
        var spent = world.Cards[Pay(world, "res")];

        Assert.Throws<RulesNotImplementedException>(
            () => CardPlay.Play(world, printed, new Silent(), seat, expensive,
                [spent.ObjectId], []));

        Assert.Same(seat.Hand, spent.Area);
        Assert.Same(seat.Hand, expensive.Area);
    }

    [Rule("rr:cost.3")]
    [Fact]
    public void ACardCannotPayForItself()
    {
        // It is leaving the hand to be played, and `rr:cost.3` spends resources
        // "by discarding cards from their hand".
        var printed = Cards();
        var world = Board(printed);
        var free = InHand(world, "free");

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => CardPlay.Play(
                world, printed, new Silent(), world.Seats[0], free, [free.ObjectId], []));

        Assert.Contains("cannot also pay for itself", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:cost.3")]
    [Fact]
    public void ACardOutsideTheHandCannotBeSpent()
    {
        // "A player spends resources that they generate by **discarding cards
        // from their hand**." A card in the deck, in play, or in somebody
        // else's hand is not a generator, and reaching for one would be
        // discarding a card the player was never holding.
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var ally = InHand(world, "ally");
        var inDeck = seat.Deck.Cards[0];

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => CardPlay.Play(
                world, printed, new Silent(), seat, ally, [inDeck.ObjectId], []));

        Assert.Contains("is not in p0's hand", thrown.Message, StringComparison.Ordinal);
        Assert.Same(seat.Hand, ally.Area);
        Assert.Same(seat.Deck, inDeck.Area);
    }

    [Rule("rr:player-turn.2")]
    [Rule("rr:resource-card")]
    [Fact]
    public void AResourceCardIsNotPlayable()
    {
        // `rr:player-turn.2` lists "an ally, upgrade, support, or player side
        // scheme card" and a resource card is not among them: its "primary
        // function is to be discarded from a player's hand to generate
        // resources". `01088` Energy prints no cost at all.
        var printed = Cards();
        var world = Board(printed);
        var resource = InHand(world, "res");

        Assert.Null(CardPlay.Price(world, printed, world.Seats[0], resource));
        Assert.Throws<RulesNotImplementedException>(
            () => CardPlay.Play(
                world, printed, new Silent(), world.Seats[0], resource, [], []));
    }

    [Rule("rr:play-put-into-play.1")]
    [Fact]
    public void AFormOnlyCardNeedsThatForm()
    {
        // "Cards with the text '[type] form only' can only be played or put
        // into play by a player whose identity is in the specified form."
        var printed = Cards().With("suited", ("Cost", "0"), ("Form", "Suit"));
        var world = Board(printed);
        var seat = world.Seats[0];
        var card = InHand(world, "suited");

        Assert.Null(CardPlay.Price(world, printed, seat, card));

        // A faceup card in play granting that form makes it playable.
        world.CreateCard(
            "suited2",
            world.AreaOf(
                DeckType.UpgradesArea, seat.IdentityCard.Area.PlayArea,
                seat.IdentityCard.ObjectId, cardOwner: 0));

        Assert.NotNull(CardPlay.Price(world, printed, seat, card));
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void ACardNobodyCanPayForIsNotOffered()
    {
        // "Determine the cost [...] and **the player's ability to pay them**."
        // An affordance that would throw when taken is worse than an absent one.
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var expensive = InHand(world, "expensive");

        Assert.Null(CardPlay.Price(world, printed, seat, expensive));

        for (int spare = 0; spare < 9; spare++)
        {
            InHand(world, "res");
        }

        Assert.NotNull(CardPlay.Price(world, printed, seat, expensive));
    }

    /// <summary>One object id of a card in hand with the given face.</summary>
    private static int Pay(World world, string faceId) =>
        world.Seats[0].Hand.Cards.First(card => card.FaceId == faceId).ObjectId;

    private static Card InHand(World world, string faceId) =>
        world.CreateCard(faceId, world.Seats[0].Hand);

    private static World Board(Printed printed)
    {
        var world = new World(printed, players: 1);
        world.CreateSeat("p0");
        world.Seats[0].IdentityCard = world.CreateCard("alterego,hero", world.Seats[0].Hero);

        // A deck with cards in it, because an empty one plus a discard pile
        // gaining its first card is `rr:player-deck.4` -- the deck resets and
        // the card just discarded goes straight back into it, which is correct
        // and not what any of these tests is about.
        for (int card = 0; card < 5; card++)
        {
            world.CreateCard("res", world.Seats[0].Deck);
        }

        for (int card = 0; card < 4; card++)
        {
            world.CreateCard("res", world.Seats[0].Hand);
        }

        return world;
    }

    private static Printed Cards() => new Printed()
        .With("res", ("RES", "GG"))
        .With("ally", ("Cost", "1"), ("RES", "R"))
        .With("upgrade", ("Cost", "2"), ("RES", "B"))
        .With("event", ("Cost", "0"), ("RES", "Y"))
        .With("free", ("Cost", "0"), ("RES", "Y"))
        .With("expensive", ("Cost", "9"), ("RES", "B"))
        .With("suited2", ("Form", "Suit"));

    private sealed class Silent : ICardAbilities
    {
        public IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player) => [];

        public IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) => [];

        public IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability) => [];

        public Marvel.Rules.Prompts.Affordance Describe(World world, PendingAbility ability) =>
            throw new NotSupportedException();
    }

    private sealed class Counting : ICardAbilities
    {
        public int Resolved { get; private set; }

        public IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player)
        {
            Resolved += 1;
            return [];
        }

        public IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) => [];

        public IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability) => [];

        public Marvel.Rules.Prompts.Affordance Describe(World world, PendingAbility ability) =>
            throw new NotSupportedException();
    }

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
            "alterego" => CardKind.AlterEgo,
            "hero" => CardKind.Hero,
            "res" => CardKind.Resource,
            "ally" => CardKind.Ally,
            "event" => CardKind.Event,
            _ => CardKind.Upgrade,
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

        public string? FormKeyword(string faceId) =>
            Attributes(faceId).TryGetValue("Form", out string? form)
                ? form.ToLowerInvariant()
                : null;
    }
}
