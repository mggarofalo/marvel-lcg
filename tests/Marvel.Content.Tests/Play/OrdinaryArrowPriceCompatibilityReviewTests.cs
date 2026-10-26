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
    }
}
