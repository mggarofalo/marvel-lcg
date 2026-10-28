using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual card classifications and Power text; synthetic actions isolate cost ownership.
public sealed class PowerAbilityExecutionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cost.3.1")]
    public void LeadershipSupportAbilityCanUseBothPowerResources(bool explicitAllocation)
    {
        // rr:cost.3.1: resources spent to pay an ability on a card count as paid for that card.
        var runner = Runner("01073", "GG");
        Card? owner = null;
        Card? power = null;
        var (_, world) = Playing(board =>
        {
            owner = board.CreateCard("01073", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            power = board.CreateCard("01072", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), candidate => candidate.Card == owner!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal("G", Assert.Single(price.Generators, generator => generator.Effect == power!.ObjectId).Generates);
        Assert.Equal("G", Assert.Single(price.Generators, generator => generator.Effect == power!.ObjectId).ConditionalBonus?.Generates);
        runner.ExecuteAction(world, new PlayerAction(action, [power!.ObjectId], [],
            Allocations: explicitAllocation ? [new(power.ObjectId, 0, "GG")] : []));
        Assert.Equal(DeckType.DiscardPile, power.Area.Type);
    }

    [Theory]
    [InlineData("01069", 1)]
    [InlineData("01070", 0)]
    [Rule("rr:cost.3.1")]
    public void LeadershipEventAppliesPowerToPrintedOrArrowComponent(string face, int powerComponent)
    {
        var runner = Runner(face, face == "01069" ? "GG" : "Y");
        Card? eventCard = null;
        Card? power = null;
        Card? energy = null;
        var (_, world) = Playing(board =>
        {
            eventCard = board.CreateCard(face, board.Seats[0].Hand);
            power = board.CreateCard("01072", board.Seats[0].Hand);
            energy = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), candidate => candidate.Card == eventCard!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal(3UL, Assert.Single(price.Generators, generator => generator.Effect == power!.ObjectId).ConditionalBonus?.ComponentMask);
        int[] selected = face == "01069" ? [power!.ObjectId] : [power!.ObjectId, energy!.ObjectId];
        ResourceAllocation[] allocation = face == "01069" ? [new(power!.ObjectId, powerComponent, "GG")]
            : [new(power!.ObjectId, powerComponent, "GG"), new(energy!.ObjectId, 1, "Y")];
        runner.ExecuteAction(world, new PlayerAction(action, selected, [], Allocations: allocation));
        Assert.Equal(DeckType.DiscardPile, power!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, eventCard!.Area.Type);
    }

    private static AbilityRunner Runner(string owner, string cost) => new(AbilityCatalog.Parse("""
        {"cards":[
          {"card":"01072","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
            "effect":{"doubleResourceFor":"Leadership"}}]},
          {"card":"OWNER","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
            "cost":{"spend":"COST"},"effect":{"draw":{"player":"you","count":1}}}]}
        ]}
        """.Replace("OWNER", owner, StringComparison.Ordinal).Replace("COST", cost, StringComparison.Ordinal)));
}
