using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class CardPlayAnotherPlayersCharacterTests : CardPlayTestBase
{
    [Rule("rr:friendly")]
    [Fact]
    public void AnotherPlayersCharacterIsFriendlyToo()
    {
        // `rr:friendly` is one sentence -- "a blanket term that refers to cards
        // **the players** control" -- so the other player's Wasp is the Wasp
        // this card needs. Unreachable at one player, and the reason a team-up
        // card is a card about a table.
        var printed = Cards();
        var world = new World(printed, players: 2);
        world.CreateSeat("p0");
        world.CreateSeat("p1");
        world.Seats[0].IdentityCard = world.CreateCard("Ant-Man", world.Seats[0].Hero);
        world.Seats[1].IdentityCard = world.CreateCard("Wasp", world.Seats[1].Hero);
        var card = world.CreateCard("swarm", world.Seats[0].Hand);
        Assert.NotNull(CardPayment.Price(world, printed, world.Seats[0], card));
    }

    [Rule("rr:identity.4")]
    [Rule("rr:in-play-and-out-of-play.1")]
    [Rule("rr:in-play-and-out-of-play.6")]
    [Fact]
    public void OnlyTheFaceupSideOfAnIdentityIsInPlay()
    {
        // "The faceup side of an identity card is considered to be in play. The
        // facedown side [...] is considered to be out of play." More generally,
        // the faceup side of a double-sided card is in play. So a player whose
        // alter-ego is showing is not the hero the card names.
        var printed = Cards();
        var world = Board(printed);
        var identity = world.CreateCard("alterego,Wasp", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = identity;
        world.CreateCard("Ant-Man", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var card = InHand(world, "swarm");
        identity.TurnTo("Wasp");
        Assert.NotNull(CardPayment.Price(world, printed, world.Seats[0], card));
        // Flipped down, and the same card is now unplayable. The two faces of
        // an identity print different titles and only one of them is on the
        // table.
        identity.TurnTo("alterego");
        Assert.Null(CardPayment.Price(world, printed, world.Seats[0], card));
    }

    [Rule("rr:unique-icon.1.2")]
    [Fact]
    public void ASlashNamesOneCharacterByTwoOfItsNames()
    {
        // "Heart of the Panther" prints *Team-Up (Black Panther/T'Challa and
        // Black Panther/Shuri)*, because two identities share the hero title
        // Black Panther and the alter-ego is what tells them apart. **No card
        // is titled "Black Panther/T'Challa"**, so the notation is read rather
        // than matched -- and it is read against every face of the identity,
        // because neither face carries both halves.
        //
        // `rr:unique-icon.1.2` is why that is not a liberty: the rules already
        // use an identity's alter-ego title as one of its identifying names.
        var printed = Cards();
        var world = new World(printed, players: 2);
        world.CreateSeat("p0");
        world.CreateSeat("p1");
        world.Seats[0].IdentityCard = world.CreateCard("Black Panther,T'Challa", world.Seats[0].Hero);
        var card = world.CreateCard("panther", world.Seats[0].Hand);
        // One of the two, so far.
        Assert.Null(CardPayment.Price(world, printed, world.Seats[0], card));
        var other = world.CreateCard("Black Panther,Shuri", world.Seats[1].Hero);
        world.Seats[1].IdentityCard = other;
        Assert.NotNull(CardPayment.Price(world, printed, world.Seats[0], card));
        // And it stays true when they flip: which face is up decides what is in
        // play, and it does not decide which character the identity *is*.
        other.TurnTo("Shuri");
        Assert.NotNull(CardPayment.Price(world, printed, world.Seats[0], card));
    }

    [Rule("rr:alliance")]
    [Rule("rr:alliance.1")]
    [Rule("rr:alliance.2")]
    [Fact]
    public void AnAllianceCardCanBePaidForByTheWholeTable()
    {
        // "When a player declares their intention to play a card with the
        // alliance keyword, **any player(s) may help pay the costs** for that
        // card." Three of the cost sit in the other player's hand, so without
        // the keyword there is no way to play it and with it there is.
        var printed = Cards();
        var world = Table(printed);
        var mine = world.CreateCard("res", world.Seats[0].Hand);
        var theirs = world.CreateCard("res", world.Seats[1].Hand);
        var card = world.CreateCard("together", world.Seats[0].Hand);
        var solo = world.CreateCard("alone", world.Seats[0].Hand);
        // One card in hand generates two of a cost of three, so neither card
        // is payable alone.
        Assert.Null(CardPayment.Price(world, printed, world.Seats[0], solo));
        var price = Assert.IsType<CostOption>(CardPayment.Price(world, printed, world.Seats[0], card));
        Assert.Equal([mine.ObjectId, theirs.ObjectId], (price.Sources ?? []).Select(source => source.Effect));
        CardPlay.Play(world, printed, new Silent(), world.Seats[0], card, [mine.ObjectId, theirs.ObjectId], []);
        // **Each spent card goes to its own owner's discard pile.** Helping to
        // pay does not make the card yours.
        Assert.Equal(0, mine.Owner);
        Assert.Equal(1, theirs.Owner);
        Assert.Equal(DeckType.DiscardPile, mine.Area.Type);
        Assert.Equal(DeckType.DiscardPile, theirs.Area.Type);
        Assert.NotSame(mine.Area, theirs.Area);
    }

    [Rule("rr:loses")]
    [Rule("rr:alliance")]
    [Fact]
    public void ACardThatLosesAllianceCannotUseAnotherPlayersResources()
    {
        // Alliance remains printed, but while the keyword is lost only the
        // playing player's hand can contribute to the payment.
        var printed = Cards();
        var world = Table(printed);
        world.CreateCard("res", world.Seats[0].Hand);
        world.CreateCard("res", world.Seats[1].Hand);
        var card = world.CreateCard("together", world.Seats[0].Hand);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("alliance"), Affects: card.ObjectId));
        Assert.Null(CardPayment.Price(world, printed, world.Seats[0], card));
    }

    [Rule("rr:cost.3")]
    [Fact]
    public void ACardWithoutAllianceCannotReachAcrossTheTable()
    {
        // The converse, and the reason alliance is a keyword: `rr:cost.3`
        // spends resources "by discarding cards from **their** hand", so
        // ordinarily another player's hand is not a place a payment can come
        // from at all.
        var printed = Cards();
        var world = Table(printed);
        world.CreateCard("res", world.Seats[0].Hand);
        var theirs = world.CreateCard("res", world.Seats[1].Hand);
        var card = world.CreateCard("alone", world.Seats[0].Hand);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Silent(), world.Seats[0], card, [theirs.ObjectId], []));
        Assert.Contains("is not in p0's hand", thrown.Message, StringComparison.Ordinal);
        Assert.Same(world.Seats[1].Hand, theirs.Area);
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
        var thrown = Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Silent(), world.Seats[0], free, [free.ObjectId], []));
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
        var thrown = Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Silent(), seat, ally, [inDeck.ObjectId], []));
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
        Assert.Null(CardPayment.Price(world, printed, world.Seats[0], resource));
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Silent(), world.Seats[0], resource, [], []));
    }

    [Rule("rr:form-change-form.7")]
    [Rule("rr:play-put-into-play.1")]
    [Rule("rr:play-restrictions-and-permissions")]
    [Rule("rr:play-restrictions-and-permissions.1")]
    [Fact]
    public void AFormOnlyCardNeedsThatForm()
    {
        // "Cards with the text '[type] form only' can only be played or put
        // into play by a player whose identity is in the specified form."
        var printed = Cards().With("suited", ("Cost", "0"), ("RequiredForm", "Suit"));
        var world = Board(printed);
        var seat = world.Seats[0];
        var card = InHand(world, "suited");
        Assert.Null(CardPayment.Price(world, printed, seat, card));
        // A faceup card in play granting that form makes it playable.
        world.CreateCard("suited2", world.AreaOf(DeckType.UpgradesArea, seat.IdentityCard.Area.PlayArea, seat.IdentityCard.ObjectId, cardOwner: 0));
        Assert.NotNull(CardPayment.Price(world, printed, seat, card));
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
        Assert.Null(CardPayment.Price(world, printed, seat, expensive));
        for (int spare = 0; spare < 9; spare++)
        {
            InHand(world, "res");
        }

        Assert.NotNull(CardPayment.Price(world, printed, seat, expensive));
    }

    [Rule("rr:toughness")]
    [Rule("rr:uses-x-type")]
    [Fact]
    public void APlayedCardRunsItsEntersPlayKeywords()
    {
        // The keywords that fire when a card enters play do not care how it got
        // there -- `rr:enters-play` is about entering, not about revealing.
        // Eighteen allies in the pool print `rr:toughness`, so a played one that
        // skipped them would arrive without its tough status card.
        var printed = Cards().With("bruiser", ("Cost", "0"), ("RES", "R"), ("HP", "3"), ("Toughness", "1")).With("gadget", ("Cost", "0"), ("RES", "R"), ("Uses", "3,web"));
        var world = Board(printed);
        var seat = world.Seats[0];
        var ally = InHand(world, "bruiser");
        CardPlay.Play(world, printed, new Silent(), seat, ally, [], []);
        Assert.True(Statuses.Has(world, ally, Statuses.Tough));
        var upgrade = InHand(world, "gadget");
        CardPlay.Play(world, printed, new Silent(), seat, upgrade, [], []);
        Assert.Equal(3, upgrade.Tokens["c_web"]);
    }

    [Rule("rr:restricted")]
    [Rule("rr:restricted.1")]
    [Fact]
    public void AThirdRestrictedCardAsksWhichOneLeavesPlay()
    {
        // "A player **can** play or put into play a restricted card even if
        // they already control two restricted cards. However, if a player ever
        // controls more than two [...] they must **immediately** choose and
        // discard from play restricted cards they control until they have only
        // two."
        //
        // So it is not a play restriction: the third card goes into play and
        // then one leaves. `rr:restricted.1` is a **Forced Response** for that
        // reason.
        var printed = Cards().With("locked", ("Cost", "0"), ("RES", "R"), ("Restricted", "1"));
        var world = Board(printed);
        var seat = world.Seats[0];
        var first = InHand(world, "locked");
        CardPlay.Play(world, printed, new Silent(), seat, first, [], []);
        var second = InHand(world, "locked");
        CardPlay.Play(world, printed, new Silent(), seat, second, [], []);
        Assert.Equal(DeckType.UpgradesArea, first.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, second.Area.Type);
        var third = InHand(world, "locked");
        var abilities = new Silent();
        CardPlay.Play(world, printed, abilities, seat, third, [], []);
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, printed, abilities, events);
        Assert.NotNull(asked);
        Assert.Equal(Question.Element, asked.Asking);
        Assert.Equal(seat.Index, asked.Player);
        Assert.Equal([first.ObjectId, second.ObjectId, third.ObjectId], asked.Affordances.Select(option => option.Id));
        Sequence.Answer(world, printed, abilities, asked, Decision.Take(second.ObjectId), events);
        Assert.Null(Sequence.Work(world, printed, abilities, events));
        Assert.Equal(DeckType.UpgradesArea, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, third.Area.Type);
    }

    [Rule("rr:restricted")]
    [Fact]
    public void TwoRestrictedCardsAreFine()
    {
        var printed = Cards().With("locked", ("Cost", "0"), ("RES", "R"), ("Restricted", "1"));
        var world = Board(printed);
        var seat = world.Seats[0];
        var first = InHand(world, "locked");
        CardPlay.Play(world, printed, new Silent(), seat, first, [], []);
        var second = InHand(world, "locked");
        CardPlay.Play(world, printed, new Silent(), seat, second, [], []);
        Assert.Equal(DeckType.UpgradesArea, first.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, second.Area.Type);
    }
}
