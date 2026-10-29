using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class ScopedCardCostReductionTests : CardPlayTestBase
{
    [Rule("rr:play-put-into-play")]
    [Fact]
    public void ScopedPlayPaysReducedCostAndDoesNotDiscountNextCard()
    {
        // rr:play-put-into-play: playing pays the cost and places the card in play.
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var card = InHand(world, "ally");
        var next = InHand(world, "ally");
        var reduction = new ScopedCardCostReduction(world, seat, card, 1);
        var request = new ScopedCardPlayRequest(printed, new Silent(), seat, card,
            new Decision(card.ObjectId, [], []), reduction);

        ScopedPermanentPlay.Validate(world, request);
        CardPlay.PlayScoped(world, request, []);
        Assert.Null(Sequence.Work(world, printed, new Silent(), []));

        Assert.Equal(DeckType.AlliesArea, card.Area.Type);
        Assert.Equal(1, CardPayment.CostOf(world, printed, seat, next).Amount);
        Assert.Equal(4, seat.Hand.Cards.Count(c => c.FaceId == "res"));

        var payment = seat.Hand.Cards.First(c => c.FaceId == "res");
        CardPlay.PlayAllocated(world, printed, new Silent(), seat, next,
            new Decision(next.ObjectId, [], [payment.ObjectId]), []);
        Assert.Null(Sequence.Work(world, printed, new Silent(), []));
        Assert.Equal(DeckType.AlliesArea, next.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(3, seat.Hand.Cards.Count(c => c.FaceId == "res"));
    }

    [Fact]
    public void ScopedPlayRejectsAStalePaymentBeforeMutation()
    {
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var card = InHand(world, "bruiser");
        var spent = seat.Hand.Cards[0];
        var request = new ScopedCardPlayRequest(printed, new Silent(), seat, card,
            new Decision(card.ObjectId, [], [spent.ObjectId]), new(world, seat, card, 1));
        World.MoveToTop(spent, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var before = world.Digest().Canonical();
        var agenda = world.Agenda.Count;

        Assert.Throws<ArgumentException>(() => CardPlay.PlayScoped(world, request, []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(agenda, world.Agenda.Count);
        Assert.Same(seat.Hand, card.Area);
    }

    [Rule("rr:modifiers.4")]
    [Fact]
    public void ScopedReductionFloorsAtZeroAndDoesNotChangeAnotherQuote()
    {
        // rr:modifiers.4: a value below zero "is treated as zero".
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var ally = InHand(world, "ally");
        var free = InHand(world, "support");
        var allyReduction = new ScopedCardCostReduction(world, seat, ally, 1);
        var freeReduction = new ScopedCardCostReduction(world, seat, free, 1);
        var before = world.Digest().Canonical();

        Assert.Equal(0, CardPayment.CostOf(world, printed, seat, ally, null, allyReduction).Amount);
        Assert.Equal(0, CardPayment.CostOf(world, printed, seat, free, null, freeReduction).Amount);
        Assert.Equal("0", CardPayment.Price(world, printed, seat, ally, null, allyReduction)!.Cost);
        Assert.Equal("1", CardPayment.Price(world, printed, seat, ally)!.Cost);
        Assert.Equal(1, CardPayment.CostOf(world, printed, seat, ally).Amount);
        Assert.Empty(world.Effects.Active());
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Rule("rr:modifiers.2")]
    [Fact]
    public void ScopedReductionStacksWithOrdinaryModifierWithoutConsumingIt()
    {
        // rr:modifiers.2: the calculation treats modifiers as simultaneous.
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var source = world.CreateCard("free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var card = InHand(world, "bruiser");
        CardPayment.ReduceNextCardCost(world, source, 0, 1);
        var reduction = new ScopedCardCostReduction(world, seat, card, 1);

        var adjusted = CardPayment.CostOf(world, printed, seat, card, null, reduction);
        Assert.Equal(1, adjusted.Amount);
        Assert.Single(adjusted.Modifiers);
        Assert.Single(world.Effects.Active());
        Assert.Equal(2, CardPayment.CostOf(world, printed, seat, card).Amount);
    }

    [Rule("rr:modifiers.2")]
    [Fact]
    public void ScopedPlayConsumesAnExistingOneUseModifierOnlyForThatPlay()
    {
        // rr:modifiers.2: the calculation treats modifiers as simultaneous.
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var source = world.CreateCard("free", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var card = InHand(world, "bruiser");
        var next = InHand(world, "ally");
        var payment = seat.Hand.Cards.First(c => c.FaceId == "res");
        CardPayment.ReduceNextCardCost(world, source, 0, 1);
        var reduction = new ScopedCardCostReduction(world, seat, card, 1);
        var price = CardPayment.Price(world, printed, seat, card, null, reduction)!;
        var spent = new[] { payment.ObjectId };

        CardPlay.PlayScoped(world, new ScopedCardPlayRequest(printed, new Silent(), seat, card,
            new Decision(card.ObjectId, [], spent, Allocations: ResourcePayment.Allocate(price, spent)), reduction), []);
        Assert.Null(Sequence.Work(world, printed, new Silent(), []));

        Assert.Equal(DeckType.AlliesArea, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Empty(world.Effects.Active());
        Assert.Equal(1, CardPayment.CostOf(world, printed, seat, next).Amount);
    }

    [Fact]
    public void ScopeRejectsANewInPlayIncarnation()
    {
        var printed = Cards();
        var world = Board(printed);
        var seat = world.Seats[0];
        var card = InHand(world, "ally");
        var reduction = new ScopedCardCostReduction(world, seat, card, 1);
        World.MoveToTop(card, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        World.MoveToTop(card, seat.Hand);

        Assert.Throws<ArgumentException>(() => CardPayment.CostOf(world, printed, seat, card, null, reduction));
        Assert.Equal(1, CardPayment.CostOf(world, printed, seat, card).Amount);
    }

    [Fact]
    public void ScopeRejectsAnotherPhysicalCardOrSeat()
    {
        var printed = Cards();
        var world = Table(printed);
        var card = world.CreateCard("ally", world.Seats[0].Hand);
        var other = world.CreateCard("ally", world.Seats[0].Hand);
        var reduction = new ScopedCardCostReduction(world, world.Seats[0], card, 1);

        Assert.Throws<ArgumentException>(() => CardPayment.CostOf(world, printed, world.Seats[0], other, null, reduction));
        Assert.Throws<ArgumentException>(() => CardPayment.CostOf(world, printed, world.Seats[1], card, null, reduction));
    }
}

