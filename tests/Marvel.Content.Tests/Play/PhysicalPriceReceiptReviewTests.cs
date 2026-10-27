using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic payment-dependent bodies review the physical-only prerequisite, not Hulk card admission.</summary>
public sealed class PhysicalPriceReceiptReviewTests
{
    [Theory]
    [InlineData("G", false)]
    [InlineData("R", true)]
    [Rule("rr:wild-resource.1")]
    public void PreflightCannotUseOneWildAsBothPhysicalPriceAndWildReceipt(string type, bool offered)
    {
        // A wild may specify "which resource type ... it is being used as".
        // A receipt declared wild cannot simultaneously satisfy the physical-only price.
        var (world, _, card) = PhysicalPrintedPriceTests.Board();
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"10014","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "printedCostResource":"R",
              "effect":{"if":{"test":{"paidWithResource":"TYPE"},
                "then":{"placeCounters":{"card":"you","counter":"test","count":1}},
                "else":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}}}}]}]}
            """.Replace("TYPE", type, StringComparison.Ordinal)));
        Assert.Null(world.TheCardIn(DeckType.MainSchemesArea));
        foreach (var held in world.Seats[0].Hand.Cards.Where(held => held != card).ToArray())
            World.MoveToTop(held, world.Seats[0].Deck);
        var physical = world.CreateCard("01090", world.Seats[0].Hand);
        var wild = world.CreateCard("01055", world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();

        var actions = runner.Actions(world, 0).Where(action => action.Card == card.ObjectId).ToArray();

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        if (!offered)
        {
            Assert.Empty(actions);
            return;
        }
        var action = Assert.Single(actions);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        int[] sources = [physical.ObjectId, wild.ObjectId];
        var allocation = ResourcePayment.Allocate(price, sources);
        Assert.NotNull(allocation);
        runner.ExecuteAction(world, new PlayerAction(action, sources, [], Allocations: allocation));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }
}
