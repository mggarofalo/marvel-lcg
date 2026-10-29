using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic action isolates the scoped printed-cost reduction from an arrow cost.
public sealed class ScopedEventPaymentTests
{
    [Fact]
    public void ReductionChangesOnlyPrintedComponentAndPaymentUsesRealResources()
    {
        var (program, runner) = PaymentRunner("01005", """{"draw":{"player":"you","count":1}}""", """{"spendAny":1}""");
        var (world, _) = OrderedPaymentBoard(runner);
        var seat = world.Seats[0];
        var card = world.CreateCard("01005", seat.Hand);
        var energy = world.CreateCard("01088", seat.Hand);
        var mental = world.CreateCard("01089", seat.Hand);
        var ability = Assert.Single(program.On(card.FaceId));
        var reduction = new ScopedCardCostReduction(world, seat, card, 1);
        var reduced = AbilityPaymentPricing.CombinedPrice(world, card, 0, ability, runner, reduction)!;
        var ordinary = AbilityPaymentPricing.CombinedPrice(world, card, 0, ability, runner)!;
        Assert.Equal("3", reduced.Cost);
        Assert.Equal("2", reduced.ResourceCosts[0].Cost);
        Assert.Equal("1", reduced.ResourceCosts[1].Cost);
        Assert.Equal("4", ordinary.Cost);
        var allocations = new[] { new ResourceAllocation(energy.ObjectId, 0, "YY"), new ResourceAllocation(mental.ObjectId, 1, "B") };
        var selection = AbilityPaymentSelection.Capture([energy.ObjectId, mental.ObjectId], [], allocations: allocations);
        var payment = Assert.IsType<AbilityEventPayment>(AbilityEventPayment.Prepare(world, card, 0, ability, runner, selection, reduction));
        Assert.True(payment.Commit(new Occurrence(0, [Steps.TurnAction], Subject: card.ObjectId, Player: 0), []));
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
    }

    [Fact]
    public void ZeroPrintedCostStaysZeroAndArrowRemainsPayable()
    {
        var (program, runner) = PaymentRunner("01005", """{"draw":{"player":"you","count":1}}""", """{"spendAny":1}""");
        var (world, _) = OrderedPaymentBoard(runner);
        var seat = world.Seats[0];
        var card = world.CreateCard("01005", seat.Hand);
        var ability = Assert.Single(program.On(card.FaceId));
        var reduction = new ScopedCardCostReduction(world, seat, card, 4);
        var price = AbilityPaymentPricing.CombinedPrice(world, card, 0, ability, runner, reduction)!;
        Assert.Equal("1", price.Cost);
        Assert.Equal("0", price.ResourceCosts[0].Cost);
        Assert.Equal("1", price.ResourceCosts[1].Cost);
    }
    [Fact]
    public void ScopedQuoteRejectsAnotherPhysicalEvent()
    {
        var (program, runner) = PaymentRunner("01005", """{"draw":{"player":"you","count":1}}""", """{"spendAny":1}""");
        var (world, _) = OrderedPaymentBoard(runner);
        var seat = world.Seats[0];
        var first = world.CreateCard("01005", seat.Hand);
        var second = world.CreateCard("01005", seat.Hand);
        var ability = Assert.Single(program.On(first.FaceId));
        var reduction = new ScopedCardCostReduction(world, seat, first, 1);
        Assert.Throws<ArgumentException>(() => AbilityPaymentPricing.CombinedPrice(world, second, 0, ability, runner, reduction));
        Assert.Equal("4", AbilityPaymentPricing.CombinedPrice(world, second, 0, ability, runner)!.Cost);
    }

    [Fact]
    public void VariableEnergyArrowStillRequiresARealEnergyAfterReduction()
    {
        var (program, runner) = PaymentRunner("01005", """{"draw":{"player":"you","count":1}}""", """{"spendEnergyX":"Y"}""");
        var (world, _) = OrderedPaymentBoard(runner);
        var seat = world.Seats[0];
        var card = world.CreateCard("01005", seat.Hand);
        var energy = world.CreateCard("01088", seat.Hand);
        var ability = Assert.Single(program.On(card.FaceId));
        var reduction = new ScopedCardCostReduction(world, seat, card, 3);
        var price = AbilityPaymentPricing.CombinedPrice(world, card, 0, ability, runner, reduction)!;
        Assert.Equal("0", price.ResourceCosts[0].Cost);
        Assert.Equal("X", price.ResourceCosts[1].Cost);
        var selection = AbilityPaymentSelection.Capture([energy.ObjectId], [], new Dictionary<string, long> { ["X"] = 1 },
            allocations: [new ResourceAllocation(energy.ObjectId, 1, "Y")]);
        var payment = Assert.IsType<AbilityEventPayment>(AbilityEventPayment.Prepare(world, card, 0, ability, runner, selection, reduction));
        Assert.True(payment.Commit(new Occurrence(0, [Steps.TurnAction], Subject: card.ObjectId, Player: 0), []));
        Assert.Equal(1, payment.Energy);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
    }

}
