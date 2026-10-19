using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class GenericResourceCostTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void EventPriceIncludesPrintedAndUnrestrictedArrowCosts(int generic)
    {
        var runner = Runner("01005", "Action", """{"draw":{"player":"you","count":1}}""",
            cost: $$"""{"spendAny":{{generic}}}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) Discard.Card(world, card, "test", []);
        var source = world.CreateCard("01005", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var mental = world.CreateCard("01089", world.Seats[0].Hand);
        if (generic == 2)
            Assert.DoesNotContain(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var wild = world.CreateCard("01055", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal((3 + generic).ToString(System.Globalization.CultureInfo.InvariantCulture), price.Cost);
        Assert.Equal(2, price.ResourceCosts.Count);
        string before = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [energy.ObjectId], [])));
        Assert.Equal(before, world.Digest().Canonical());
        int[] paying = generic == 1 ? [energy.ObjectId, mental.ObjectId] : [energy.ObjectId, mental.ObjectId, wild.ObjectId];
        var allocations = new List<ResourceAllocation>
        {
            new(energy.ObjectId, Cost: 0, PaidAs: "YY"), new(mental.ObjectId, Cost: 0, PaidAs: "B"),
            new(mental.ObjectId, Cost: 1, PaidAs: "B"),
        };
        if (generic == 2) allocations.Add(new(wild.ObjectId, Cost: 1, PaidAs: "G"));
        runner.ExecuteAction(world, new PlayerAction(action, paying, [], Allocations: allocations));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
        Assert.Equal(generic == 2 ? DeckType.DiscardPile : DeckType.HandsArea, wild.Area.Type);
    }
}
