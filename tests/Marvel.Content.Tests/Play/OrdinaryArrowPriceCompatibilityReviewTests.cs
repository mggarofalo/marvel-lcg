using Marvel.Rules.State;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Engine choice: ordinary prices retain their replay-v24 prompt shape.</summary>
public sealed class OrdinaryArrowPriceCompatibilityReviewTests
{
    [Theory]
    [InlineData("{\"spend\":\"Y\"}")]
    [InlineData("{\"spendAny\":2}")]
    public void OrdinarySingleArrowCostRetainsImplicitComponent(string cost)
    {
        var runner = Runner("01006", "Action", """{"draw":{"player":"you","count":1}}""", cost: cost);
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01006"), abilities: runner);
        world.CreateCard("01088", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        // CostOptionRecord.From persists Components as null or an explicit list;
        // JournalReplay requires that exact value when matching saved prompts.
        Assert.Null(price.Components);
        Assert.Equal(cost.Contains("spendAny", StringComparison.Ordinal) ? [""] : ["Y"], price.Rule);
    }

    [Fact]
    public void CombinedEventGenericArrowRetainsItsEmptyStringRequirement()
    {
        var runner = Runner("01005", "Action", """{"draw":{"player":"you","count":1}}""",
            cost: """{"spendAny":2}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = world.CreateCard("01005", world.Seats[0].Hand);
        foreach (string face in new[] { "01088", "01089", "01090" })
            world.CreateCard(face, world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal("5", price.Cost);
        Assert.Equal(["", ""], price.Rule);
        Assert.Collection(price.Components!,
            printed => { Assert.Equal("3", printed.Cost); Assert.Null(printed.Rule); },
            arrow => { Assert.Equal("2", arrow.Cost); Assert.Equal([""], arrow.Rule); });
    }

    [Theory]
    [InlineData("{\"seq\":[{\"spendAny\":1},{\"spend\":\"Y\"}]}", "", "Y")]
    [InlineData("{\"seq\":[{\"spend\":\"Y\"},{\"spendAny\":1}]}", "Y", "")]
    [InlineData("{\"seq\":[{\"spendAny\":1},{\"spendAny\":1}]}", "", "")]
    [InlineData("{\"seq\":[{\"spend\":\"Y\"},{\"spend\":\"B\"}]}", "Y", "B")]
    public void SimultaneousOrdinaryArrowRetainsGenericComponentRequirement(string cost, string first, string second)
    {
        var runner = Runner("01006", "Action", """{"draw":{"player":"you","count":1}}""",
            cost: cost);
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01006"), abilities: runner);
        world.CreateCard("01088", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal("2", price.Cost);
        Assert.Collection(price.Components!,
            component => Assert.Equal([first], component.Rule),
            component => Assert.Equal([second], component.Rule));
    }

    [Theory]
    [InlineData("{\"spendAny\":2}", "")]
    [InlineData("{\"spend\":\"YB\"}", "YB")]
    [InlineData("{\"seq\":[{\"spendAny\":1},{\"spend\":\"Y\"}]}", "Y")]
    [InlineData("{\"seq\":[{\"spend\":\"Y\"},{\"spendAny\":1}]}", "Y")]
    [InlineData("{\"seq\":[{\"spendAny\":1},{\"spendAny\":1}]}", "")]
    [InlineData("{\"seq\":[{\"spend\":\"Y\"},{\"spend\":\"B\"}]}", "YB")]
    public void CombinedEventWithOrdinarySequenceRetainsOneAggregateArrowComponent(string cost, string requirement)
    {
        var runner = Runner("01005", "Action", """{"draw":{"player":"you","count":1}}""",
            cost: cost);
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = world.CreateCard("01005", world.Seats[0].Hand);
        foreach (string face in new[] { "01088", "01089", "01090" })
            world.CreateCard(face, world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal("5", price.Cost);
        Assert.Equal(["", requirement], price.Rule);
        // Engine choice: CombinedPrice captures the printed cost
        // plus one aggregate arrow cost, even when that arrow is a sequence.
        Assert.Collection(price.Components!,
            printed => { Assert.Equal("3", printed.Cost); Assert.Null(printed.Rule); },
            arrow => { Assert.Equal("2", arrow.Cost); Assert.Equal([requirement], arrow.Rule); });
    }

    [Theory]
    [InlineData("{\"spendAny\":2}")]
    [InlineData("{\"spend\":\"YB\"}")]
    [InlineData("{\"seq\":[{\"spendAny\":1},{\"spend\":\"Y\"}]}")]
    [InlineData("{\"seq\":[{\"spend\":\"Y\"},{\"spendAny\":1}]}")]
    [InlineData("{\"seq\":[{\"spendAny\":1},{\"spendAny\":1}]}")]
    [InlineData("{\"seq\":[{\"spend\":\"Y\"},{\"spend\":\"B\"}]}")]
    public void CombinedEventWithOrdinarySequenceAcceptsItsAdvertisedAllocation(string cost)
    {
        var runner = Runner("01005", "Action", """
            {"if":{"test":{"paidWithResource":"Y"},
                "then":{"draw":{"player":"you","count":1}}}}
            """, cost: cost);
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = world.CreateCard("01005", world.Seats[0].Hand);
        var payment = new List<int>();
        foreach (string face in new[] { "01088", "01089", "01090" })
            payment.Add(world.CreateCard(face, world.Seats[0].Hand).ObjectId);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        var allocation = ResourcePayment.Allocate(price, payment);
        Assert.NotNull(allocation);
        int held = world.Seats[0].Hand.Cards.Count;
        runner.ExecuteAction(world, new PlayerAction(action, payment, [], Allocations: allocation));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.All(payment, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
        // The paid-resource receipt includes the allocated energy, enabling the draw.
        Assert.Equal(held - payment.Count, world.Seats[0].Hand.Cards.Count);
    }
}
