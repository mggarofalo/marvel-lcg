using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
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

    [Rule("rr:initiating-abilities.step.3")]
    [Rule("rr:initiating-abilities.step.4")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void TheNextCardCostReductionIsPricedAndSpentOnlyByAPlay()
    {
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var source = world.CreateCard(
            "free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ally = InHand(world, "ally");
        CardPlay.ReduceNextCardCost(world, source, player: 0, amount: 1);

        // Looking at the price is not playing the card and does not spend the
        // one use. Repricing gives the same answer.
        Assert.Equal("0", CardPlay.Price(world, printed, seat, ally)!.Cost);
        Assert.Equal("0", CardPlay.Price(world, printed, seat, ally)!.Cost);
        Assert.Single(world.Effects.Active());

        CardPlay.Play(world, printed, new Silent(), seat, ally, [], []);

        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void AFailedPaymentDoesNotSpendTheNextCardReduction()
    {
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var source = world.CreateCard(
            "free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var expensive = InHand(world, "expensive");
        CardPlay.ReduceNextCardCost(world, source, player: 0, amount: 1);

        Assert.Throws<RulesNotImplementedException>(() =>
            CardPlay.Play(world, printed, new Silent(), seat, expensive, [], []));

        Assert.Single(world.Effects.Active());
        Assert.Equal("8", CardPlay.Price(world, printed, seat, expensive)!.Cost);
    }

    [Rule("rr:target.5")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void ACostReductionBelongsOnlyToTheChosenPlayer()
    {
        var printed = Cards();
        var world = Table(printed);
        var source = world.CreateCard(
            "free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var mine = world.CreateCard("ally", world.Seats[0].Hand);
        var theirs = world.CreateCard("ally", world.Seats[1].Hand);
        CardPlay.ReduceNextCardCost(world, source, player: 1, amount: 1);

        Assert.Equal(1, CardPlay.CostOf(world, printed, world.Seats[0], mine).Amount);
        Assert.Equal(0, CardPlay.CostOf(world, printed, world.Seats[1], theirs).Amount);
    }

    [Rule("rr:initiating-abilities.step.7")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void ACardThatDoesNotFinishBeingPlayedDoesNotSpendTheReduction()
    {
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var source = world.CreateCard(
            "free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var upgrade = InHand(world, "upgrade");
        var payment = world.Cards[Pay(world, "res")];
        CardPlay.ReduceNextCardCost(world, source, player: 0, amount: 1);

        // This upgrade requires one specific host, and the forged play names
        // none. Its payment succeeds, but step 7 never says the card was
        // played, so "the next card you play" has not happened.
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(
            world, printed, new Targets(seat.IdentityCard.ObjectId), seat, upgrade,
            [payment.ObjectId], [], targets: []));

        Assert.Single(world.Effects.Active());
        Assert.Same(seat.Hand, upgrade.Area);
    }

    [Rule("rr:modifiers.2")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void CostReductionsStackAtZeroAndEachUseIsSpent()
    {
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var source = world.CreateCard(
            "free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ally = InHand(world, "ally");
        CardPlay.ReduceNextCardCost(world, source, player: 0, amount: 2);
        CardPlay.ReduceNextCardCost(world, source, player: 0, amount: 2);

        Assert.Equal("0", CardPlay.Price(world, printed, seat, ally)!.Cost);

        CardPlay.Play(world, printed, new Silent(), seat, ally, [], []);

        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:lasting-effects.5")]
    [Fact]
    public void AnUnusedCardCostReductionExpiresWithThePlayerPhase()
    {
        var printed = Cards();
        var world = Board(printed);
        var source = world.CreateCard(
            "free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ally = InHand(world, "ally");
        CardPlay.ReduceNextCardCost(world, source, player: 0, amount: 1);

        Assert.Equal(1, world.Effects.Expire(TimingPoints.EndOfPlayerPhase));

        Assert.Equal("1", CardPlay.Price(world, printed, world.Seats[0], ally)!.Cost);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:play-put-into-play.3")]
    [Rule("rr:play-put-into-play.4")]
    [Rule("rr:play-put-into-play.5")]
    [Rule("rr:ownership-and-control.3")]
    [Rule("rr:ownership-and-control.7.2")]
    [Fact]
    public void AnOwnedAllyCanEnterPlayUnderAnotherPlayersControl()
    {
        // Putting a card into play ignores its resource cost but still uses a
        // legal destination: this cost-three ally enters the controller's ally
        // area without payment. It is not considered to have been played.
        var printed = Cards();
        var world = Table(printed);
        var owner = world.Seats[1];
        var discard = world.AreaOf(
            DeckType.DiscardPile, PlayArea.Of(owner.Index), cardOwner: owner.Index);
        var ally = world.CreateCard("bruiser", discard);
        var events = new List<GameEvent>();

        CardPlay.PutAllyIntoPlay(
            world, printed, new Silent(), ally, controller: 0,
            trigger: "Make_The_Call", events);

        Assert.Equal(1, ally.Owner);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(PlayArea.Of(0), ally.Area.PlayArea);
        Assert.True(Statuses.Has(world, ally, Statuses.Tough));
        Assert.Contains(events.OfType<ControlChanged>(), changed =>
            changed.Card == ally.ObjectId && changed.From == 1 && changed.To == 0);
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == Steps.CardPlayed);
    }

    [Fact]
    public void AConditionalGeneratorSeesTheCardBeingPaidForInOfferAndResolution()
    {
        // The rules describe each card's conditional resource text, but they
        // do not prescribe the engine API. The engine passes the payment target
        // both while pricing and while spending so those two answers cannot
        // disagree.
        var printed = Cards()
            .With("power", ("RES", "G"))
            .With("matching", ("Cost", "2"), ("RES", "B"))
            .With("other", ("Cost", "2"), ("RES", "B"));
        var world = Board(printed);
        Empty(world);
        var source = InHand(world, "power");
        var matching = InHand(world, "matching");
        world.Abilities = new ConditionalResources("matching");

        var price = CardPlay.Price(world, printed, world.Seats[0], matching);

        Assert.Equal("GG", Assert.Single(price!.Generators).Generates);
        CardPlay.Play(
            world, printed, new Silent(), world.Seats[0], matching, [source.ObjectId], []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);

        var otherWorld = Board(printed);
        Empty(otherWorld);
        var otherSource = InHand(otherWorld, "power");
        var other = InHand(otherWorld, "other");
        otherWorld.Abilities = new ConditionalResources("matching");

        Assert.Null(CardPlay.Price(otherWorld, printed, otherWorld.Seats[0], other));
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(
            otherWorld, printed, new Silent(), otherWorld.Seats[0], other,
            [otherSource.ObjectId], []));
        Assert.Same(otherWorld.Seats[0].Hand, otherSource.Area);
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

    [Rule("rr:requirement-resources")]
    [Fact]
    public void ACardWithARequirementIsNotOfferedWithoutTheResource()
    {
        // "A card with the requirement keyword cannot be played unless each
        // resource of the specified type is spent while paying for that card's
        // cost." A hand of the wrong type pays the *number* and not the card,
        // so this is not offered at all -- an affordance that would throw when
        // taken is worse than an absent one.
        var printed = Cards();
        var world = Board(printed);
        Empty(world);
        InHand(world, "mental");
        InHand(world, "mental");
        var card = InHand(world, "demanding");

        Assert.Null(CardPlay.Price(world, printed, world.Seats[0], card));

        // And taking it anyway is refused by name rather than half-paid.
        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => CardPlay.Play(
                world, printed, new Silent(), world.Seats[0], card,
                [world.Seats[0].Hand.Cards[0].ObjectId], []));
        Assert.Contains("requiring 'R'", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:requirement-resources.1")]
    [Fact]
    public void TheRequiredResourceIsPartOfTheCostRatherThanExtra()
    {
        // A cost of 1 requiring a physical is **one** card that generates a
        // physical, not one plus a physical -- the same reading `rr:resource.4`
        // gets, and the reason `Pays` takes the requirement rather than adding
        // to the number.
        var printed = Cards();
        var world = Board(printed);
        Empty(world);
        var paying = InHand(world, "physical");
        var card = InHand(world, "demanding");

        Assert.NotNull(CardPlay.Price(world, printed, world.Seats[0], card));
        CardPlay.Play(
            world, printed, new Silent(), world.Seats[0], card, [paying.ObjectId], []);

        Assert.Equal(DeckType.DiscardPile, paying.Area.Type);
    }

    [Rule("rr:team-up")]
    [Fact]
    public void ATeamUpCardNeedsBothOfTheCharactersItNames()
    {
        // "A card with the team-up keyword cannot be played unless **both** of
        // the named friendly characters *(identity or ally)* are in play."
        // One is not both, which is the half a looser reading would allow.
        var printed = Cards();
        var world = Board(printed);
        var card = InHand(world, "swarm");
        var allies = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);

        Assert.Null(CardPlay.Price(world, printed, world.Seats[0], card));

        world.CreateCard("Ant-Man", allies);
        Assert.Null(CardPlay.Price(world, printed, world.Seats[0], card));

        world.CreateCard("Wasp", allies);
        Assert.NotNull(CardPlay.Price(world, printed, world.Seats[0], card));
    }

    [Rule("rr:team-up.2")]
    [Fact]
    public void AnAllyCountsUnderItsSubtitleToo()
    {
        // "An ally counts as a named character if **either its title or
        // subtitle** matches the named character." Wasp's ally card is titled
        // for one of her names and subtitled for the other, and the card that
        // names her does not say which.
        var printed = Cards();
        var world = Board(printed);
        var card = InHand(world, "swarm");
        var allies = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("Ant-Man", allies);
        world.CreateCard("Janet", allies);

        Assert.NotNull(CardPlay.Price(world, printed, world.Seats[0], card));
    }

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

        Assert.NotNull(CardPlay.Price(world, printed, world.Seats[0], card));
    }

    [Rule("rr:identity.4")]
    [Fact]
    public void OnlyTheFaceupSideOfAnIdentityIsInPlay()
    {
        // "The faceup side of an identity card is considered to be in play. The
        // facedown side [...] is considered to be out of play." So a player
        // whose alter-ego is showing is not the hero the card names, however
        // sure the table is about who they are.
        var printed = Cards();
        var world = Board(printed);
        var identity = world.CreateCard("alterego,Wasp", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = identity;
        world.CreateCard(
            "Ant-Man", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var card = InHand(world, "swarm");

        identity.TurnTo("Wasp");
        Assert.NotNull(CardPlay.Price(world, printed, world.Seats[0], card));

        // Flipped down, and the same card is now unplayable. The two faces of
        // an identity print different titles and only one of them is on the
        // table.
        identity.TurnTo("alterego");
        Assert.Null(CardPlay.Price(world, printed, world.Seats[0], card));
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
        world.Seats[0].IdentityCard =
            world.CreateCard("Black Panther,T'Challa", world.Seats[0].Hero);
        var card = world.CreateCard("panther", world.Seats[0].Hand);

        // One of the two, so far.
        Assert.Null(CardPlay.Price(world, printed, world.Seats[0], card));

        var other = world.CreateCard("Black Panther,Shuri", world.Seats[1].Hero);
        world.Seats[1].IdentityCard = other;
        Assert.NotNull(CardPlay.Price(world, printed, world.Seats[0], card));

        // And it stays true when they flip: which face is up decides what is in
        // play, and it does not decide which character the identity *is*.
        other.TurnTo("Shuri");
        Assert.NotNull(CardPlay.Price(world, printed, world.Seats[0], card));
    }

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
        Assert.Null(CardPlay.Price(world, printed, world.Seats[0], solo));

        var price = Assert.IsType<CostOption>(
            CardPlay.Price(world, printed, world.Seats[0], card));
        Assert.Equal(
            [mine.ObjectId, theirs.ObjectId],
            (price.Sources ?? []).Select(source => source.Effect));

        CardPlay.Play(
            world, printed, new Silent(), world.Seats[0], card,
            [mine.ObjectId, theirs.ObjectId], []);

        // **Each spent card goes to its own owner's discard pile.** Helping to
        // pay does not make the card yours.
        Assert.Equal(0, mine.Owner);
        Assert.Equal(1, theirs.Owner);
        Assert.Equal(DeckType.DiscardPile, mine.Area.Type);
        Assert.Equal(DeckType.DiscardPile, theirs.Area.Type);
        Assert.NotSame(mine.Area, theirs.Area);
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

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => CardPlay.Play(
                world, printed, new Silent(), world.Seats[0], card, [theirs.ObjectId], []));

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

    [Rule("rr:form-change-form.7")]
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

    [Rule("rr:toughness")]
    [Rule("rr:uses-x-type")]
    [Fact]
    public void APlayedCardRunsItsEntersPlayKeywords()
    {
        // The keywords that fire when a card enters play do not care how it got
        // there -- `rr:enters-play` is about entering, not about revealing.
        // Eighteen allies in the pool print `rr:toughness`, so a played one that
        // skipped them would arrive without its tough status card.
        var printed = Cards()
            .With("bruiser", ("Cost", "0"), ("RES", "R"), ("HP", "3"), ("Toughness", "1"))
            .With("gadget", ("Cost", "0"), ("RES", "R"), ("Uses", "3,web"));
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
    public void AThirdRestrictedCardForcesTheOldestOut()
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
        CardPlay.Play(world, printed, new Silent(), seat, third, [], []);

        // The one just played stays, and the oldest of the others goes.
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, second.Area.Type);
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

    [Rule("rr:ownership-and-control.2.1")]
    [Rule("rr:ownership-and-control.7.2")]
    [Rule("rr:upgrade.3.1")]
    [Fact]
    public void AnUpgradeAttachedToAnotherPlayersCardIsTheirsUntilItLeavesPlay()
    {
        // An upgrade on another player's card is controlled by that player,
        // but leaving play still sends it to its owner's equivalent out-of-play
        // area. The two play areas make control visible; the discard piles make
        // ownership visible.
        var printed = Cards().With("shared", ("Cost", "0"), ("RES", "R"));
        var world = Table(printed);
        var owner = world.Seats[0];
        var controller = world.Seats[1];
        var upgrade = world.CreateCard("shared", owner.Hand);
        var abilities = new Targets(controller.IdentityCard.ObjectId);

        CardPlay.Play(
            world, printed, abilities, owner, upgrade, [], [],
            [controller.IdentityCard.ObjectId]);

        Assert.Equal(0, upgrade.Owner);
        Assert.Equal(1, upgrade.Area.PlayArea.Player);
        Assert.Equal(controller.IdentityCard.ObjectId, upgrade.Area.Host);

        Discard.Card(world, upgrade, "test", []);

        Assert.Same(
            world.AreaOf(DeckType.DiscardPile, PlayArea.Of(owner.Index)),
            upgrade.Area);
    }

    [Rule("rr:ownership-and-control.2")]
    [Fact]
    public void APlayerUpgradeAttachedToAnEncounterCardStaysUnderItsOwnersControl()
    {
        // Encounter cards belong to the scenario, but the rule only transfers
        // an attached upgrade to "a player other than the upgrade's owner."
        // An upgrade such as Webbed Up remains in its owner's play area while
        // its host sits in the villain's.
        var printed = Cards().With("web", ("Cost", "0"), ("RES", "R"));
        var world = Board(printed);
        var villain = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        var upgrade = InHand(world, "web");
        var abilities = new Targets(villain.ObjectId);

        CardPlay.Play(world, printed, abilities, world.Seats[0], upgrade, [], [],
            [villain.ObjectId]);

        Assert.Equal(0, upgrade.Area.PlayArea.Player);
        Assert.Equal(villain.ObjectId, upgrade.Area.Host);
    }

    [Fact]
    public void MaxPerPlayerRemovesTheCardFromOffersAndRejectsForgedPlay()
    {
        var printed = Cards().With("limited", ("Cost", "0"), ("MaxPerUnit", "1"));
        var world = Board(printed);
        var inPlay = world.CreateCard(
            "limited", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var copy = InHand(world, "limited");

        Assert.NotNull(inPlay);
        Assert.Null(CardPlay.Price(world, printed, world.Seats[0], copy));
        Assert.Throws<RulesNotImplementedException>(() =>
            CardPlay.Play(world, printed, new Silent(), world.Seats[0], copy, [], []));
    }

    [Fact]
    public void MaxPerPlayerUsesTheChosenUpgradeController()
    {
        var printed = Cards().With("limited", ("Cost", "0"), ("MaxPerUnit", "1"));
        var world = Table(printed);
        var copy = InHand(world, "limited");
        var first = world.Seats[0].IdentityCard;
        var second = world.Seats[1].IdentityCard;
        world.CreateCard(
            "limited",
            world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), first.ObjectId, cardOwner: 0));
        var abilities = new Targets(first.ObjectId, second.ObjectId);
        world.Abilities = abilities;

        Assert.NotNull(CardPlay.Price(world, printed, world.Seats[0], copy));
        Assert.Throws<RulesNotImplementedException>(() =>
            CardPlay.Play(
                world, printed, abilities, world.Seats[0], copy, [], [], [first.ObjectId]));

        CardPlay.Play(
            world, printed, abilities, world.Seats[0], copy, [], [], [second.ObjectId]);
        Assert.Equal(1, copy.Area.PlayArea.Player);
    }

    /// <summary>One object id of a card in hand with the given face.</summary>
    private static int Pay(World world, string faceId) =>
        world.Seats[0].Hand.Cards.First(card => card.FaceId == faceId).ObjectId;

    private static Card InHand(World world, string faceId) =>
        world.CreateCard(faceId, world.Seats[0].Hand);

    /// <summary>Clears the hand, so that a test can say what is in it.</summary>
    private static void Empty(World world)
    {
        foreach (var card in world.Seats[0].Hand.Cards.ToList())
        {
            World.MoveToTop(card, world.Seats[0].Deck);
        }
    }

    /// <summary>Two players, each with an empty hand to fill.</summary>
    private static World Table(Printed printed)
    {
        var world = new World(printed, players: 2);
        for (int seat = 0; seat < 2; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard =
                world.CreateCard("alterego,hero", world.Seats[seat].Hero);

            // A deck with cards in it, for the same reason `Board` has one:
            // `rr:player-deck.4` would otherwise reset an empty deck the moment
            // a payment reached the discard pile.
            for (int card = 0; card < 5; card++)
            {
                world.CreateCard("filler", world.Seats[seat].Deck);
            }
        }

        return world;
    }

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
        .With("bruiser", ("Cost", "3"), ("HP", "3"), ("Toughness", "1"))
        .With("upgrade", ("Cost", "2"), ("RES", "B"))
        .With("support", ("Cost", "0"), ("RES", "B"))
        .With("event", ("Cost", "0"), ("RES", "Y"))
        .With("free", ("Cost", "0"), ("RES", "Y"))
        .With("expensive", ("Cost", "9"), ("RES", "B"))
        .With("suited2", ("Form", "Suit"))

        // `rr:requirement-resources` -- thirteen cards in the pool print one.
        // `27006` requires a mental, `27016` a physical, `27049` one of each of
        // energy, mental and physical.
        .With("physical", ("RES", "R"))
        .With("mental", ("RES", "B"))
        .With("demanding", ("Cost", "1"), ("RES", "R"), ("Requirement", "R"))

        // `rr:team-up` -- 28 cards print one, and every one names two heroes.
        .With("swarm", ("Cost", "0"), ("TeamUp", "Ant-Man;Wasp"))
        .With("Ant-Man", ("HP", "9"))
        .With("Wasp", ("HP", "9"))
        .With("Janet", ("HP", "3"))
        .Sub("Janet", "Wasp")
        .With("panther", ("Cost", "0"), ("TeamUp", "Black Panther/T'Challa;Black Panther/Shuri"))
        .With("Black Panther", ("HP", "9"))
        .With("T'Challa", ("HP", "9"))
        .With("Shuri", ("HP", "9"))

        // `rr:alliance` -- 13 cards print one, and every one of them is a card
        // about a table.
        .With("together", ("Cost", "3"), ("Alliance", "1"))
        .With("alone", ("Cost", "3"));

    private sealed class Silent : NoCardAbilities
    {


        public override IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying, IReadOnlyList<int> chosen) => [];

    }

    private sealed class Counting : NoCardAbilities
    {
        public int Resolved { get; private set; }

        public override IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player)
        {
            Resolved += 1;
            return [];
        }


        public override IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying, IReadOnlyList<int> chosen) => [];

    }

    private sealed class Targets(params int[] targets) : NoCardAbilities
    {
        public override IReadOnlyList<int>? AttachmentTargets(World world, Card card) => targets;
    }

    private sealed class ConditionalResources(string matching) : NoCardAbilities
    {
        public override string ResourcesGeneratedBy(
            World world, Card source, Card? payingFor) =>
            payingFor?.FaceId == matching
                ? Resources.GeneratedBy(source.FaceId, world.Facts) + Resources.Wild
                : Resources.GeneratedBy(source.FaceId, world.Facts);
    }

    private sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal);

        private readonly Dictionary<string, string> subtitles = new(StringComparer.Ordinal);

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
            "ally" or "bruiser" => CardKind.Ally,
            "event" => CardKind.Event,
            "support" => CardKind.Support,
            _ => CardKind.Upgrade,
        };

        /// <summary>A printed subtitle — `rr:team-up.2` matches on it too.</summary>
        public Printed Sub(string faceId, string subtitle)
        {
            subtitles[faceId] = subtitle;
            return this;
        }

        public string Subtitle(string faceId) =>
            subtitles.TryGetValue(faceId, out string? found) ? found : string.Empty;

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
