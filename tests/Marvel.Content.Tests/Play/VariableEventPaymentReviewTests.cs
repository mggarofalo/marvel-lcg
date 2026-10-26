using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic effects challenge the shared payment contract, not Lightning Strike's effect.</summary>
public sealed class VariableEventPaymentReviewTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("unselected")]
    [InlineData("wrong-type")]
    [InlineData("wrong-component")]
    [InlineData("double-use")]
    [InlineData("duplicate-source")]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:cost.5.1")]
    public void ForgedAllocationRejectsAtomicallyAndTheSameActionCanBeRetried(string forgery)
    {
        // "Abort this process without paying any costs." The player "chooses how to divide those resources".
        var runner = Synthetic("Action", "WhenActionTriggered");
        var (_, world) = Playing(_ => { }, abilities: runner);
        foreach (var held in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(held, world.Seats[0].Deck);
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var mental = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var values = new Dictionary<string, long> { ["X"] = 2 };
        int[] selected = [energy.ObjectId, mental.ObjectId];
        ResourceAllocation[] allocation = forgery switch
        {
            "missing" => [],
            "unselected" => [new(source.ObjectId, 0, "B"), new(energy.ObjectId, 1, "YY")],
            "wrong-type" => [new(energy.ObjectId, 0, "Y"), new(mental.ObjectId, 1, "YY")],
            "wrong-component" => [new(mental.ObjectId, 0, "B"), new(energy.ObjectId, 2, "YY")],
            "double-use" => [new(energy.ObjectId, 0, "Y"), new(energy.ObjectId, 1, "YY")],
            _ => [new(mental.ObjectId, 0, "B"), new(energy.ObjectId, 1, "YY")],
        };
        if (forgery == "duplicate-source") selected = [energy.ObjectId, mental.ObjectId, energy.ObjectId];
        var before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var agenda = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, selected, [], values, allocation)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(agenda, world.Agenda.Outstanding.ToArray());
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId, mental.ObjectId], [], values,
            [new(mental.ObjectId, 0, "B"), new(energy.ObjectId, 1, "YY")]));
        Assert.Equal(2, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.All(new[] { source, energy, mental }, paid => Assert.Equal(DeckType.DiscardPile, paid.Area.Type));
    }

    [Theory]
    [InlineData("Interrupt")]
    [InlineData("Response")]
    [Rule("rr:cost.5.1")]
    public void WindowEventUsesOnePrintedAndOneEnergyIconFromTheSameResourceCard(string timing)
    {
        // One generated icon can "pay for the card's resource cost and the other" the pre-arrow cost.
        var runner = Synthetic(timing, "ReviewProbe");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var type = timing == "Interrupt" ? AbilityType.Interrupt : AbilityType.Response;
        var result = runner.ResolveWindow(world, new Occurrence(1, ["ReviewProbe"], Player: 0),
            new PendingAbility(source.ObjectId, type, 0),
            Decision.Take(source.ObjectId, [], [energy.ObjectId], new Dictionary<string, long> { ["X"] = 1 },
                [new(energy.ObjectId, 0, "Y"), new(energy.ObjectId, 1, "Y")]));
        var events = new List<GameEvent>(result.Events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.True(result.IsComplete);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void FailedAllocationRetainsThePrintedDiscountAndSuccessfulPaymentConsumesIt()
    {
        // "Abort this process without paying any costs." This includes a one-use printed-price reduction.
        var runner = Synthetic("Action", "WhenActionTriggered");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, 1);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var values = new Dictionary<string, long> { ["X"] = 2 };
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [energy.ObjectId], [], values,
                [new(energy.ObjectId, 0, "Y"), new(energy.ObjectId, 1, "Y")])));
        Assert.Equal(0, CardPayment.CostOf(world, world.Facts, world.Seats[0], source).Amount);
        Assert.Same(world.Seats[0].Hand, source.Area);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId], [], values,
            [new(energy.ObjectId, 1, "YY")]));
        Assert.Equal(2, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, world.Seats[0], source).Amount);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void VariableEnergyCannotHideAnUnsupportedCompoundEntryCost(bool entryFirst)
    {
        const string entry = "{\"discardUntilMinionEngaged\":\"you\"}";
        const string energy = "{\"spendEnergyX\":\"Y\"}";
        string cost = "{\"seq\":[" + (entryFirst ? entry + "," + energy : energy + "," + entry) + "]}";
        var failure = Assert.Throws<RulesNotImplementedException>(() => Synthetic("Action", "WhenActionTriggered", cost));
        Assert.Contains("simultaneous payment", failure.Message, StringComparison.Ordinal);
        Assert.Contains("discardUntilMinionEngaged", failure.Message, StringComparison.Ordinal);
    }

    private static AbilityRunner Synthetic(string timing, string trigger, string cost = "{\"spendEnergyX\":\"Y\"}") =>
        new(AbilityCatalog.Parse("""
            {"cards":[{"card":"06006","abilities":[{
              "trigger":{"event":"TRIGGER","timing":"TIMING","subject":"game"},
              "cost":COST,
              "effect":{"placeCounters":{"card":"you","counter":"test","count":{"result":"energy"}}}
            }]}]}
            """.Replace("TRIGGER", trigger, StringComparison.Ordinal)
                .Replace("TIMING", timing, StringComparison.Ordinal).Replace("COST", cost, StringComparison.Ordinal)));
}
