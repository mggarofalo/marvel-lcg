using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class CardPlayACostIsACountOfResourcesOfAnyTypeTests : CardPlayTestBase
{
    [Rule("rr:resource.3")]
    [Rule("rr:cost.4")]
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

    [Rule("rr:cost.4.1")]
    [Fact]
    public void ResourcesBeyondTheCostAreGeneratedButNotPaidForIt()
    {
        // Ordering is the payer's allocation: the first icon pays this
        // one-resource cost and the second is overpayment, not a second
        // resource paid for the card.
        Assert.Equal("Y", Resources.Paid("YB", cost: 1));
        // A requirement is allocated before the unrestricted remainder.
        Assert.Equal("YR", Resources.Paid("YBR", cost: 2, required: "R"));
    }

    [Rule("rr:cost.4.2")]
    [Rule("rr:resource.5")]
    [Fact]
    public void AnExcessResourceIsLostAfterTheCostIsPaid()
    {
        // The resource card generates two wilds toward a cost of one. It is
        // discarded as the generator, and the excess cannot carry into the
        // next card's cost.
        var printed = Cards();
        var world = Board(printed);
        var first = InHand(world, "ally");
        var second = InHand(world, "ally");
        int doubleWild = Pay(world, "res");
        CardPlay.Play(world, printed, new Silent(), world.Seats[0], first, [doubleWild], []);
        Assert.Equal(DeckType.DiscardPile, world.Cards[doubleWild].Area.Type);
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Silent(), world.Seats[0], second, [], []));
        Assert.Same(world.Seats[0].Hand, second.Area);
    }

    [Rule("rr:resource.2")]
    [Rule("rr:resource.4")]
    [Rule("rr:wild-resource")]
    [Rule("rr:wild-resource.1")]
    [Rule("rr:wild-resource.1.1")]
    [Rule("rr:wild-resource.2")]
    [Rule("rr:energy-resource")]
    [Rule("rr:energy-resource.1")]
    [Rule("rr:energy-resource.2")]
    [Rule("rr:mental-resource")]
    [Rule("rr:mental-resource.1")]
    [Rule("rr:mental-resource.2")]
    [Rule("rr:physical-resource")]
    [Rule("rr:physical-resource.1")]
    [Rule("rr:physical-resource.2")]
    [Theory]
    // Energy, mental and physical are three of the four resource types. Each
    // "can be spent to pay the resource cost of cards and abilities", and
    // abilities may specifically require that exact type.
    [InlineData("Y", "Y", true)]
    [InlineData("B", "B", true)]
    [InlineData("R", "R", true)]
    [InlineData("BB", "B", true)]
    [InlineData("RR", "B", false)]
    // "Wild resources can be used as their type or any of the other types."
    [InlineData("GG", "B", true)]
    [InlineData("GG", "BR", true)]
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

    [Rule("rr:wild-resource.3")]
    [Fact]
    public void APrintedWildIsNotAnotherTypeOutsidePayingACost()
    {
        var printed = Cards();
        var world = Board(printed);
        var doubleWild = world.Cards[Pay(world, "res")];
        Assert.Equal(2, Resources.PrintedCount([doubleWild], Resources.Wild, printed));
        Assert.Equal(0, Resources.PrintedCount([doubleWild], Resources.Mental, printed));
    }

    [Rule("rr:non-numerical-variable.1")]
    [Fact]
    public void ACostThatIsNotANumberSaysSoRatherThanReadingAsZero()
    {
        // For a cost of X the player chooses or card text defines X before
        // modifiers apply. Reading an unsupported X as zero would silently
        // make the card free.
        var printed = new Printed().With("odd", ("Cost", "X"));
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Resources.Cost("odd", printed));
        Assert.Contains("is not a number", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:cost.2")]
    [Rule("rr:cost.2.1")]
    [Rule("rr:per-player-icon.1")]
    [Fact]
    public void APerPlayerCostUsesTheStartingCountThenReducesTheTotal()
    {
        // A per-player resource cost is multiplied by the number who started
        // the scenario. A reduction applies to that total, and eliminating a
        // player does not change it: three times two, less one, stays five.
        var printed = Cards().With("scaling", ("Cost", "2*"));
        var world = new World(printed, players: 3);
        for (int player = 0; player < 3; player++)
        {
            world.CreateSeat($"p{player}");
            world.Seats[player].IdentityCard = world.CreateCard("identity", world.Seats[player].Hero);
        }

        var card = world.CreateCard("scaling", world.Seats[0].Hand);
        var source = world.CreateCard("support", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Seats[2].Eliminated = true;
        CardPayment.ReduceNextCardCost(world, source, player: 0, amount: 1);
        Assert.Equal(5, CardPayment.CostOf(world, printed, world.Seats[0], card).Amount);
    }

    [Rule("rr:dash-value.1")]
    [Fact]
    public void ACardWithADashCostCannotBePlayedForZero()
    {
        // A dash cost means the card cannot be played and may only enter play
        // through another effect. Treating it as a missing numeric value would
        // turn the restriction into a free card.
        var printed = Cards().With("dash", ("Cost", "–"));
        var world = Board(printed);
        var card = InHand(world, "dash");
        Assert.Null(CardPayment.Price(world, printed, world.Seats[0], card));
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Silent(), world.Seats[0], card, [], []));
        Assert.Same(world.Seats[0].Hand, card.Area);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:in-play-and-out-of-play.3")]
    [Rule("rr:player-s-play-area.1")]
    [Fact]
    public void PlayerCardsEnterTheirKindsAreaInThePlayersPlayArea()
    {
        // A card enters play when it moves from an out-of-play area to a play
        // area. Allies, supports, and upgrades all start here in hand and land
        // in their printed kind's area inside the playing player's play area.
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var ally = InHand(world, "ally");
        var events = new List<GameEvent>();
        CardPlay.Play(world, printed, new Silent(), seat, ally, [Pay(world, "res")], events);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(PlayArea.Of(0), ally.Area.PlayArea);
        var upgrade = InHand(world, "upgrade");
        CardPlay.Play(world, printed, new Silent(), seat, upgrade, [Pay(world, "res")], events);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
        Assert.Equal(PlayArea.Of(0), upgrade.Area.PlayArea);
        Assert.Equal(seat.IdentityCard.ObjectId, upgrade.Area.Host);
        Assert.Contains(events.OfType<CardAttached>(), e => e.Card == upgrade.ObjectId);
        var support = InHand(world, "support");
        CardPlay.Play(world, printed, new Silent(), seat, support, [], events);
        Assert.Equal(DeckType.SupportsArea, support.Area.Type);
        Assert.Equal(PlayArea.Of(0), support.Area.PlayArea);
    }

    [Rule("rr:player-s-play-area.5")]
    [Fact]
    public void APlayedCardGoesToItsPlayersAreaAndNotAnotherPlayers()
    {
        // Unless a rule or card ability says otherwise, a card cannot be
        // played into another player's play area. An ordinary support has no
        // such permission, so player zero's card cannot land with player one.
        var printed = Cards();
        var world = Table(printed);
        var support = world.CreateCard("support", world.Seats[0].Hand);
        CardPlay.Play(world, printed, new Silent(), world.Seats[0], support, [], []);
        Assert.Equal(PlayArea.Of(0), support.Area.PlayArea);
        Assert.NotEqual(PlayArea.Of(1), support.Area.PlayArea);
    }

    [Rule("rr:support.1")]
    [Fact]
    public void ASupportEntersTheBackRowOfItsPlayersArea()
    {
        // "Support cards enter play in the back row of a player's play area."
        // The engine names that row SupportsArea and keeps the player's seat on it.
        var printed = Cards();
        var world = Board(printed);
        var support = InHand(world, "support");
        CardPlay.Play(world, printed, new Silent(), world.Seats[0], support, [], []);
        Assert.Equal(DeckType.SupportsArea, support.Area.Type);
        Assert.Equal(PlayArea.Of(0), support.Area.PlayArea);
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

    [Rule("rr:ignore")]
    [Rule("rr:ignore.1")]
    [Fact]
    public void IgnoringAResourceCostPaysZeroAndStillPlaysTheCard()
    {
        // The support costs two, and the hand contains no generators. The
        // permission treats the cost as absent, so no payment is made and the
        // card still enters play normally.
        var printed = Cards().With("costly", ("Cost", "2"));
        var world = Board(printed);
        var support = InHand(world, "costly");
        CardPlay.PlayIgnoringResourceCost(world, printed, new Silent(), world.Seats[0], support, []);
        Assert.Equal(DeckType.UpgradesArea, support.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards);
    }

    [Rule("rr:requirement-resources.2")]
    [Fact]
    public void AResourceRequirementCannotBeIgnored()
    {
        // Ignoring a cost pays no resources, so the required physical cannot
        // be paid. The refusal happens before the card leaves the hand.
        var printed = Cards();
        var world = Board(printed);
        var card = InHand(world, "demanding");
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.PlayIgnoringResourceCost(world, printed, new Silent(), world.Seats[0], card, []));
        Assert.Same(world.Seats[0].Hand, card.Area);
    }

    [Fact]
    public void AnEventCannotUseTheNonEventIgnoredCostEntryPoint()
    {
        var printed = Cards();
        var world = Board(printed);
        var card = InHand(world, "event");
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.PlayIgnoringResourceCost(world, printed, new Silent(), world.Seats[0], card, []));
        Assert.Same(world.Seats[0].Hand, card.Area);
    }

    [Rule("rr:play-restrictions-and-permissions.2")]
    [Fact]
    public void APermissionCanPlayAnOwnedCardFromTheDiscardPile()
    {
        // A permission may allow "an ally card to be played from a player's
        // discard pile." The zone and timing are overridden; its printed cost
        // and other play restrictions remain in force.
        var printed = Cards().With("ally", ("Cost", "2"));
        var world = Board(printed);
        var seat = world.Seats[0];
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var first = world.Cards[Pay(world, "res")];
        var second = world.Cards[Pay(world, "res")];
        CardPlay.PlayWithPermission(world, printed, new Silent(), seat, ally, [first.ObjectId, second.ObjectId], []);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
    }

    [Fact]
    public void AnEventPermissionRaisesBeforeItsActionCanBeMisresolved()
    {
        var printed = Cards();
        var world = Board(printed);
        var card = world.CreateCard("event", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.PlayWithPermission(world, printed, new Silent(), world.Seats[0], card, [], []));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }
}
