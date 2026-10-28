using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Direct payment fixtures exercise actual printed resources without pack admission.
public sealed class PowerResourceGenerationTests
{
    [Theory]
    [InlineData("12011")]
    [InlineData("01069")]
    [Rule("rr:cost.4")]
    public void ZeroCostLeadershipLeavesPowerWhollyExcess(string targetFace)
    {
        // rr:cost.4: excess resources are "not paid for that cost".
        // Ruling 5e6ac90dc4187eef confirms one Power resource for Hank.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var seat = world.Seats[0];
        var target = world.CreateCard(targetFace, seat.Hand);
        var power = world.CreateCard("01072", seat.Hand);
        Assert.Equal(0, CardPayment.CostOf(world, world.Facts, seat, target).Amount);
        var source = Assert.Single(CardPayment.Generators(world, world.Facts, seat, runner, target),
            candidate => candidate.Effect == power.ObjectId);
        Assert.Equal("G", ResourcePayment.GeneratedBy(source, []));
        var receipt = CardResourcePayments.Spend(world, runner, new CardResourcePaymentRequest
        {
            Hands = [seat.Hand], Paying = [power.ObjectId], Cost = 0, Required = "",
            Itself = target.ObjectId, Payer = 0, PayingFor = target,
            Price = new CostOption(target.ObjectId, "0", Sources: [source]),
        }, []);
        Assert.True(receipt.IsComplete);
        Assert.Equal("", receipt.Paid);
        Assert.Equal("G", receipt.Generated);
        Assert.Equal(DeckType.DiscardPile, power.Area.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:cost.5.1")]
    public void MixedAllocationControlsPowerGenerationAtCommit(bool powerPays)
    {
        // rr:cost.5.1: the player "chooses how to divide" simultaneous resources.
        // Ruling b796aaa9fae1580b distinguishes payment from excess.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var seat = world.Seats[0];
        var target = world.CreateCard("01070", seat.Hand);
        var power = world.CreateCard("01072", seat.Hand);
        var energy = world.CreateCard("01088", seat.Hand);
        var sources = CardPayment.Generators(world, world.Facts, seat, runner, target);
        var powerQuote = Assert.Single(sources, source => source.Effect == power.ObjectId);
        var energyQuote = Assert.Single(sources, source => source.Effect == energy.ObjectId);
        Assert.Equal("YY", energyQuote.Generates);
        Assert.Null(energyQuote.ConditionalBonus);
        ResourceAllocation[] allocation = powerPays ? [new(power.ObjectId, 0, "GG")]
            : [new(energy.ObjectId, 0, "YY")];
        Assert.Equal(powerPays ? "GG" : "G", ResourcePayment.GeneratedBy(powerQuote, allocation));
        var receipt = CardResourcePayments.Spend(world, runner, new CardResourcePaymentRequest
        {
            Hands = [seat.Hand], Paying = [power.ObjectId, energy.ObjectId], Cost = 2, Required = "",
            Itself = target.ObjectId, Payer = 0, PayingFor = target,
            Price = new CostOption(target.ObjectId, "2", Sources: sources), Allocations = allocation,
        }, []);
        Assert.True(receipt.IsComplete);
        Assert.Equal(powerPays ? "GG" : "YY", receipt.Paid);
        Assert.Equal(powerPays ? "GGYY" : "GYY", receipt.Generated);
        Assert.Equal(DeckType.DiscardPile, power.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
    }

    [Fact]
    public void ReducedToZeroMakesPowerWhollyExcess()
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var seat = world.Seats[0];
        var target = world.CreateCard("01070", seat.Hand);
        var power = world.CreateCard("01072", seat.Hand);
        CardPayment.ReduceNextCardCost(world, seat.IdentityCard, 0, 2);
        long cost = CardPayment.CostOf(world, world.Facts, seat, target).Amount;
        Assert.Equal(0, cost);
        var quote = Assert.Single(CardPayment.Generators(world, world.Facts, seat, runner, target),
            source => source.Effect == power.ObjectId);
        Assert.Equal("G", ResourcePayment.GeneratedBy(quote, []));
        var receipt = CardPlay.Spend(world, world.Facts, runner, [seat.Hand], [power.ObjectId],
            cost, "", target.ObjectId, 0, [], target);
        Assert.True(receipt.IsComplete);
        Assert.Equal("", receipt.Paid);
        Assert.Equal("G", receipt.Generated);
        Assert.Equal(DeckType.DiscardPile, power.Area.Type);
    }

    [Fact]
    public void SolePowerNecessarilyPaysAndNeedsNoAllocation()
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var seat = world.Seats[0];
        var target = world.CreateCard("01070", seat.Hand);
        var power = world.CreateCard("01072", seat.Hand);
        var receipt = CardPlay.Spend(world, world.Facts, runner, [seat.Hand], [power.ObjectId],
            2, "", target.ObjectId, 0, [], target);
        Assert.True(receipt.IsComplete);
        Assert.Equal("GG", receipt.Paid);
        Assert.Equal("GG", receipt.Generated);
        Assert.Equal(DeckType.DiscardPile, power.Area.Type);
    }

    [Fact]
    public void OmittedPositiveAllocationRejectsBeforeDiscard()
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var seat = world.Seats[0];
        var target = world.CreateCard("01070", seat.Hand);
        var power = world.CreateCard("01072", seat.Hand);
        var energy = world.CreateCard("01088", seat.Hand);
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Spend(world, world.Facts, runner,
            [seat.Hand], [power.ObjectId, energy.ObjectId], 2, "", target.ObjectId, 0, [], target));
        Assert.Same(seat.Hand, power.Area);
        Assert.Same(seat.Hand, energy.Area);
    }
}
