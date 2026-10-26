using Marvel.Rules.State;
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

    [Fact]
    public void SimultaneousOrdinaryArrowRetainsGenericComponentRequirement()
    {
        var runner = Runner("01006", "Action", """{"draw":{"player":"you","count":1}}""",
            cost: """{"seq":[{"spendAny":1},{"spend":"Y"}]}""");
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, "01006"), abilities: runner);
        world.CreateCard("01088", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal("2", price.Cost);
        Assert.Collection(price.Components!,
            generic => Assert.Equal([""], generic.Rule),
            typed => Assert.Equal(["Y"], typed.Rule));
    }
}
