using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class ScopedCardCostReductionTests : CardPlayTestBase
{
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

