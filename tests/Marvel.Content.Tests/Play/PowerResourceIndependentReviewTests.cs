using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PowerResourceIndependentReviewTests
{
    [Fact]
    [Rule("rr:cost.4.1")]
    public void OnePowerIconPaysAndItsSecondIconIsExcess()
    {
        // rr:cost.4.1: excess resources are not paid for that cost.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var seat = world.Seats[0];
        var target = world.CreateCard("01070", seat.Hand);
        var power = world.CreateCard("01072", seat.Hand);
        var sources = CardPayment.Generators(world, world.Facts, seat, runner, target);
        var receipt = CardResourcePayments.Spend(world, runner, new CardResourcePaymentRequest
        {
            Hands = [seat.Hand], Paying = [power.ObjectId], Cost = 1, Required = "",
            Itself = target.ObjectId, Payer = 0, PayingFor = target,
            Price = new CostOption(target.ObjectId, "1", Sources: sources),
            Allocations = [new(power.ObjectId, 0, "G")],
        }, []);
        Assert.True(receipt.IsComplete);
        Assert.Equal("G", receipt.Paid);
        Assert.Equal("GG", receipt.Generated);
        Assert.Equal(DeckType.DiscardPile, power.Area.Type);
    }

    [Fact]
    public void RejectedAmbiguousPaymentPreservesStateAndCanBeRetried()
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var seat = world.Seats[0];
        var target = world.CreateCard("01070", seat.Hand);
        var power = world.CreateCard("01072", seat.Hand);
        var energy = world.CreateCard("01088", seat.Hand);
        var before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var current = world.Agenda.Current;
        var occurrence = world.Agenda.Occurrence;
        int pending = world.Agenda.Count;
        var sources = CardPayment.Generators(world, world.Facts, seat, runner, target);
        var price = new CostOption(target.ObjectId, "2", Sources: sources);
        Assert.Throws<RulesNotImplementedException>(() => CardResourcePayments.Spend(world, runner,
            new CardResourcePaymentRequest
            {
                Hands = [seat.Hand], Paying = [power.ObjectId, energy.ObjectId], Cost = 2, Required = "",
                Itself = target.ObjectId, Payer = 0, PayingFor = target, Price = price,
            }, []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Count);
        Assert.Equal(current, world.Agenda.Current);
        Assert.Same(occurrence, world.Agenda.Occurrence);
        var receipt = CardResourcePayments.Spend(world, runner, new CardResourcePaymentRequest
        {
            Hands = [seat.Hand], Paying = [power.ObjectId, energy.ObjectId], Cost = 2, Required = "",
            Itself = target.ObjectId, Payer = 0, PayingFor = target, Price = price,
            Allocations = [new(energy.ObjectId, 0, "YY")],
        }, []);
        Assert.True(receipt.IsComplete);
        Assert.Equal("YY", receipt.Paid);
        Assert.Equal("GYY", receipt.Generated);
        Assert.Equal(DeckType.DiscardPile, power.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
    }
}
