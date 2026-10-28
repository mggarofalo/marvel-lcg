using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EventAllocationIndependentReviewTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:cost.3")]
    [Rule("rr:cost.5")]
    public void ArrowResourcesCountAsPaidForTheEvent(bool mentalArrow)
    {
        // rr:cost.3.1: resources paid for an ability are "paid for that card".
        // rr:cost.5.1: the player "chooses how to divide" simultaneous costs.
        var runner = Runner("01005", "Action", """{"if":{"test":{"paidWithResource":"B"},"then":{"placeCounters":{"card":"you","counter":"test","count":1}}}}""", cost: """{"spendAny":1}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        foreach (var held in world.Seats[0].Hand.Cards.ToArray()) Discard.Card(world, held, "test", []);
        var card = world.CreateCard("01005", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var physical = world.CreateCard("01090", world.Seats[0].Hand);
        var arrow = mentalArrow ? world.CreateCard("01089", world.Seats[0].Hand) : physical;
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == card.ObjectId);
        int[] paying = mentalArrow ? [energy.ObjectId, physical.ObjectId, arrow.ObjectId] : [energy.ObjectId, physical.ObjectId];
        ResourceAllocation[] allocations = [new(energy.ObjectId, 0, "YY"), new(physical.ObjectId, 0, "R"), new(arrow.ObjectId, 1, mentalArrow ? "B" : "R")];
        runner.ExecuteAction(world, new PlayerAction(action, paying, [], Allocations: allocations));
        Assert.Equal(mentalArrow ? 1 : 0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }
}
