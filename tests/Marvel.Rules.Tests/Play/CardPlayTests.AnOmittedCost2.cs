using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class CardPlayAnOmittedCostTests : CardPlayTestBase
{
    [Rule("rr:dash-value.1")]
    [Fact]
    public void AnOmittedCostCannotBePlayedForZero()
    {
        // Generated card data omits the Cost field for a card whose printed
        // dash says it cannot be played. Missing is not an implicit zero.
        var printed = Cards();
        var world = Board(printed);
        var card = InHand(world, "uncosted");
        Assert.Null(CardPayment.Price(world, printed, world.Seats[0], card));
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Silent(), world.Seats[0], card, [], []));
        Assert.Same(world.Seats[0].Hand, card.Area);
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
        var source = world.CreateCard("free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ally = InHand(world, "ally");
        CardPayment.ReduceNextCardCost(world, source, player: 0, amount: 1);
        // Looking at the price is not playing the card and does not spend the
        // one use. Repricing gives the same answer.
        Assert.Equal("0", CardPayment.Price(world, printed, seat, ally)!.Cost);
        Assert.Equal("0", CardPayment.Price(world, printed, seat, ally)!.Cost);
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
        var source = world.CreateCard("free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var expensive = InHand(world, "expensive");
        CardPayment.ReduceNextCardCost(world, source, player: 0, amount: 1);
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Silent(), seat, expensive, [], []));
        Assert.Single(world.Effects.Active());
        Assert.Equal("8", CardPayment.Price(world, printed, seat, expensive)!.Cost);
    }

    [Rule("rr:target.5")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void ACostReductionBelongsOnlyToTheChosenPlayer()
    {
        var printed = Cards();
        var world = Table(printed);
        var source = world.CreateCard("free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var mine = world.CreateCard("ally", world.Seats[0].Hand);
        var theirs = world.CreateCard("ally", world.Seats[1].Hand);
        CardPayment.ReduceNextCardCost(world, source, player: 1, amount: 1);
        Assert.Equal(1, CardPayment.CostOf(world, printed, world.Seats[0], mine).Amount);
        Assert.Equal(0, CardPayment.CostOf(world, printed, world.Seats[1], theirs).Amount);
    }

    [Rule("rr:initiating-abilities.step.7")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void ACardThatDoesNotFinishBeingPlayedDoesNotSpendTheReduction()
    {
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var source = world.CreateCard("free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var upgrade = InHand(world, "upgrade");
        var payment = world.Cards[Pay(world, "res")];
        CardPayment.ReduceNextCardCost(world, source, player: 0, amount: 1);
        // This upgrade requires one specific host, and the forged play names
        // none. Its payment succeeds, but step 7 never says the card was
        // played, so "the next card you play" has not happened.
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Targets(seat.IdentityCard.ObjectId), seat, upgrade, [payment.ObjectId], [], targets: []));
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
        var source = world.CreateCard("free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ally = InHand(world, "ally");
        CardPayment.ReduceNextCardCost(world, source, player: 0, amount: 2);
        CardPayment.ReduceNextCardCost(world, source, player: 0, amount: 2);
        Assert.Equal("0", CardPayment.Price(world, printed, seat, ally)!.Cost);
        CardPlay.Play(world, printed, new Silent(), seat, ally, [], []);
        Assert.Empty(world.Effects.Active());
    }

    [Rule("rr:lasting-effects.5")]
    [Fact]
    public void AnUnusedCardCostReductionExpiresWithThePlayerPhase()
    {
        var printed = Cards();
        var world = Board(printed);
        var source = world.CreateCard("free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ally = InHand(world, "ally");
        CardPayment.ReduceNextCardCost(world, source, player: 0, amount: 1);
        Assert.Equal(1, world.Effects.Expire(TimingPoints.EndOfPlayerPhase));
        Assert.Equal("1", CardPayment.Price(world, printed, world.Seats[0], ally)!.Cost);
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
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(owner.Index), cardOwner: owner.Index);
        var ally = world.CreateCard("bruiser", discard);
        var events = new List<GameEvent>();
        CardPlay.PutAllyIntoPlay(world, printed, new Silent(), ally, controller: 0, trigger: "Make_The_Call", events);
        Assert.Equal(1, ally.Owner);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(PlayArea.Of(0), ally.Area.PlayArea);
        Assert.True(Statuses.Has(world, ally, Statuses.Tough));
        Assert.Contains(events.OfType<ControlChanged>(), changed => changed.Card == ally.ObjectId && changed.From == 1 && changed.To == 0);
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.What == Steps.CardPlayed);
    }

    [Rule("rr:resource-ability.1.1")]
    [Fact]
    public void AConditionalGeneratorSeesTheCardBeingPaidForInOfferAndResolution()
    {
        // The rules describe each card's conditional resource text, but they
        // do not prescribe the engine API. The engine passes the payment target
        // both while pricing and while spending so those two answers cannot
        // disagree.
        var printed = Cards().With("power", ("RES", "G")).With("matching", ("Cost", "2"), ("RES", "B")).With("other", ("Cost", "2"), ("RES", "B"));
        var world = Board(printed);
        Empty(world);
        var source = InHand(world, "power");
        var matching = InHand(world, "matching");
        world.Abilities = new ConditionalResources("matching");
        var price = CardPayment.Price(world, printed, world.Seats[0], matching);
        Assert.Equal("GG", Assert.Single(price!.Generators).Generates);
        CardPlay.Play(world, printed, new Silent(), world.Seats[0], matching, [source.ObjectId], []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        var otherWorld = Board(printed);
        Empty(otherWorld);
        var otherSource = InHand(otherWorld, "power");
        var other = InHand(otherWorld, "other");
        otherWorld.Abilities = new ConditionalResources("matching");
        Assert.Null(CardPayment.Price(otherWorld, printed, otherWorld.Seats[0], other));
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(otherWorld, printed, new Silent(), otherWorld.Seats[0], other, [otherSource.ObjectId], []));
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
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Silent(), seat, expensive, [spent.ObjectId], []));
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
        Assert.Null(CardPayment.Price(world, printed, world.Seats[0], card));
        // And taking it anyway is refused by name rather than half-paid.
        var thrown = Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, printed, new Silent(), world.Seats[0], card, [world.Seats[0].Hand.Cards[0].ObjectId], []));
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
        Assert.NotNull(CardPayment.Price(world, printed, world.Seats[0], card));
        CardPlay.Play(world, printed, new Silent(), world.Seats[0], card, [paying.ObjectId], []);
        Assert.Equal(DeckType.DiscardPile, paying.Area.Type);
    }

    [Rule("rr:loses")]
    [Rule("rr:requirement-resources")]
    [Fact]
    public void ACardThatLosesRequirementCanUseAnyResourceType()
    {
        // Requirement remains printed, but its resource restriction does not
        // function while the keyword is lost.
        var printed = Cards();
        var world = Board(printed);
        Empty(world);
        var mental = InHand(world, "mental");
        var card = InHand(world, "demanding");
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("requirement"), Affects: card.ObjectId));
        Assert.NotNull(CardPayment.Price(world, printed, world.Seats[0], card));
        CardPlay.Play(world, printed, new Silent(), world.Seats[0], card, [mental.ObjectId], []);
        Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
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
        Assert.Null(CardPayment.Price(world, printed, world.Seats[0], card));
        world.CreateCard("Ant-Man", allies);
        Assert.Null(CardPayment.Price(world, printed, world.Seats[0], card));
        world.CreateCard("Wasp", allies);
        Assert.NotNull(CardPayment.Price(world, printed, world.Seats[0], card));
    }

    [Rule("rr:loses")]
    [Rule("rr:team-up")]
    [Fact]
    public void ACardThatLosesTeamUpDoesNotRequireTheNamedCharacters()
    {
        // Team-Up remains printed, but the play restriction it supplies does
        // not function while the keyword is lost.
        var printed = Cards();
        var world = Board(printed);
        var card = InHand(world, "swarm");
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("team-up"), Affects: card.ObjectId));
        Assert.NotNull(CardPayment.Price(world, printed, world.Seats[0], card));
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
        Assert.NotNull(CardPayment.Price(world, printed, world.Seats[0], card));
    }
}
