using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic physical-only price on a three-cost event; no Hulk production rows.</summary>
public sealed class PhysicalPrintedPriceTests
{
    [Theory]
    [InlineData(0, "01090", "01055")]
    [InlineData(1, "01090", "")]
    [InlineData(3, "", "")]
    [InlineData(4, "", "")]
    [Rule("rr:wild-resource.1")]
    [Rule("rr:initiating-abilities.step.4")]
    public void AdjustedPrintedPriceAcceptsPhysicalAndDeclaredWild(int discount, string first, string second)
    {
        // Wild: "they may specify which resource type ... it is being used as".
        // Step 4: "Apply any modifiers to the cost(s)." 10002 restricts resources spent to physical.
        var (world, runner, card) = Board();
        if (discount > 0) CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, discount);
        var sources = new[] { first, second }.Where(face => face.Length > 0)
            .Select(face => world.CreateCard(face, world.Seats[0].Hand).ObjectId).ToArray();
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == card.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal(Math.Max(0, 3 - discount).ToString(), price.Cost);
        Assert.Equal('R', Assert.Single(price.ResourceCosts).RepeatedResource);
        var allocation = ResourcePayment.Allocate(price, sources);
        Assert.NotNull(allocation);
        Assert.All(allocation, entry => Assert.All(entry.PaidAs, icon => Assert.Equal('R', icon)));
        runner.ExecuteAction(world, new PlayerAction(action, sources, [], Allocations: allocation));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }

    [Theory]
    [InlineData("B")]
    [InlineData("G")]
    [Rule("rr:initiating-abilities.step.5")]
    public void ForgedNonPhysicalDeclarationPreservesPaymentAndDiscount(string declared)
    {
        // Abort "without paying any costs". Generated wild is physical only when declared physical.
        var (world, runner, card) = Board();
        CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, 1);
        var physical = world.CreateCard("01090", world.Seats[0].Hand);
        var wild = world.CreateCard("01055", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == card.ObjectId);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [physical.ObjectId, wild.ObjectId], [], Allocations:
                [new(physical.ObjectId, 0, "R"), new(wild.ObjectId, 0, declared)])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        Assert.Equal(2, CardPayment.CostOf(world, world.Facts, world.Seats[0], card).Amount);
    }

    [Fact]
    public void MentalOnlyHandDoesNotOfferPhysicalOnlyEvent()
    {
        var (world, runner, card) = Board();
        world.CreateCard("01089", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == card.ObjectId);
    }

    [Fact]
    [Rule("rr:cost.5.1")]
    public void PhysicalRestrictionDoesNotConstrainTheSeparateArrowCost()
    {
        // "chooses how to divide those resources between those costs".
        var (world, runner, card) = Board(arrow: true);
        var physical = world.CreateCard("01090", world.Seats[0].Hand);
        var single = world.CreateCard("10002", world.Seats[0].Hand);
        var mental = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == card.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal(2, price.ResourceCosts.Count);
        var allocation = ResourcePayment.Allocate(price, [physical.ObjectId, single.ObjectId, mental.ObjectId]);
        Assert.NotNull(allocation);
        runner.ExecuteAction(world, new PlayerAction(action,
            [physical.ObjectId, single.ObjectId, mental.ObjectId], [], Allocations: allocation));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_test"]);
    }

    internal static (World World, AbilityRunner Runner, Card Event) Board(bool arrow = false)
    {
        string cost = arrow ? "\"cost\":{\"spend\":\"B\"}," : "";
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$$"""
            {"cards":[{"card":"10014","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "printedCostResource":"R",{{{{cost}}}}
              "effect":{"placeCounters":{"card":"you","counter":"test","count":1}}}]}]}
            """));
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1, 17);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        for (int i = 0; i < 8; i++) world.CreateCard("01003", seat.Deck);
        world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        _ = Game.Begin(world, facts, runner);
        return (world, runner, world.CreateCard("10014", seat.Hand));
    }
}
