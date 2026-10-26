using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic safe effects isolate Lightning Strike's payment contract; no actual row is admitted.</summary>
public sealed class VariableEventPaymentTests
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [Rule("rr:cost.5.1")]
    [Rule("rr:cost.4.1")]
    public void PrintedPriceAndChosenEnergyUseSeparateIcons(bool discount, int x)
    {
        // "A player generating resources for those costs chooses how to divide those resources between those costs."
        // Excess resources "were not paid for that cost."
        var runner = SyntheticRunner();
        var (_, world) = Playing(_ => { }, abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(card, world.Seats[0].Deck);
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        var energy = world.CreateCard("01014", world.Seats[0].Hand);
        if (discount) CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, 1);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Collection(price.ResourceCosts,
            printed => Assert.Equal(discount ? "0" : "1", printed.Cost),
            arrow => { Assert.Equal("X", arrow.Cost); Assert.Equal('Y', arrow.RepeatedResource); });
        Assert.Equal(discount ? 3 : 2, Assert.Single(price.VariableRequests).Max);
        var values = new Dictionary<string, long> { ["X"] = x };
        var allocation = ResourcePayment.Allocate(price, [energy.ObjectId], values);
        Assert.NotNull(allocation);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId], [], values, allocation));
        Assert.Equal(x, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        if (discount) Assert.Equal(1, CardPayment.CostOf(world, world.Facts, world.Seats[0], source).Amount);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:cost.5.1")]
    public void InvalidDeclarationsLeavePaymentUntouchedAndPermitRetry()
    {
        // "If ... the cost(s) cannot be paid, abort this process without paying any costs."
        var runner = SyntheticRunner();
        var (_, world) = Playing(_ => { }, abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(card, world.Seats[0].Deck);
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var mental = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        foreach (int x in new[] { 0, 2, 3 })
        {
            var values = new Dictionary<string, long> { ["X"] = x };
            ResourceAllocation[] forged = [new(mental.ObjectId, 0, "B"), new(energy.ObjectId, 1, new string('Y', x))];
            // X=2 is affordable only when the mental source pays the printed price.
            IReadOnlyList<int> paying = x == 2 ? [energy.ObjectId] : [energy.ObjectId, mental.ObjectId];
            Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
                new PlayerAction(action, paying, [], values, forged)));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        }
        var accepted = new Dictionary<string, long> { ["X"] = 2 };
        var allocation = ResourcePayment.Allocate(price, [energy.ObjectId, mental.ObjectId], accepted);
        Assert.NotNull(allocation);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId, mental.ObjectId], [], accepted, allocation));
        Assert.Equal(2, world.Seats[0].IdentityCard.Tokens["c_test"]);
    }

    [Fact]
    public void PrintedCostCannotReuseTheOnlyAvailableEnergyIcon()
    {
        var runner = SyntheticRunner();
        var (_, world) = Playing(_ => { }, abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(card, world.Seats[0].Deck);
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        world.CreateCard("01012", world.Seats[0].Hand);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
    }

    [Fact]
    [Rule("rr:wild-resource.1")]
    public void WildDeclaredAsEnergyDoesNotAlsoPayPrintedCost()
    {
        // A wild resource "may be used as any one of the following resources".
        var runner = SyntheticRunner();
        var (_, world) = Playing(_ => { }, abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(card, world.Seats[0].Deck);
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        var wild = world.CreateCard("01055", world.Seats[0].Hand);
        var mental = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var values = new Dictionary<string, long> { ["X"] = 1 };
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [wild.ObjectId], [], values,
                [new(wild.ObjectId, 0, "B"), new(wild.ObjectId, 1, "Y")])));
        runner.ExecuteAction(world, new PlayerAction(action, [wild.ObjectId, mental.ObjectId], [], values,
            [new(mental.ObjectId, 0, "B"), new(wild.ObjectId, 1, "Y")]));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.Equal(DeckType.DiscardPile, wild.Area.Type);
    }

    [Fact]
    public void ChosenEnergySurvivesASuspendedGenerator()
    {
        var runner = SyntheticRunner();
        Card? source = null;
        Card? generator = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("06006", board.Seats[0].Hand);
            generator = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        var values = new Dictionary<string, long> { ["X"] = 1 };
        var allocation = ResourcePayment.Allocate(Assert.Single(action.CostOptions), [generator!.ObjectId], values);
        Assert.NotNull(allocation);
        game.Resolve(Decision.Take(action.Id, [], [generator.ObjectId], values, allocation));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(DeckType.RevealingArea, source!.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_test"));
        var digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(int.MaxValue)));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        values["X"] = 99;
        game.Resolve(Decision.Decline);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    private static AbilityRunner SyntheticRunner() => new(AbilityCatalog.Parse("""
        {"cards":[
          {"card":"06006","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
            "cost":{"spendEnergyX":"Y"},
            "effect":{"placeCounters":{"card":"you","counter":"test","count":{"result":"energy"}}}
          }]},
          {"card":"01030","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
            "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"YY"}
          }]},
          {"card":"01092","abilities":[{
            "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
            "effect":{"draw":{"player":"you","count":1}}
          }]}
        ]}
        """));
}
