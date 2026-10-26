using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic ordering and lifecycle controls for the bounded payment trace.</summary>
public sealed class IndependentHealingCostStateReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData("test", false)]
    [InlineData("test", true)]
    [InlineData("allPurpose", false)]
    [InlineData("allPurpose", true)]
    [Rule("rr:cost.12")]
    public void TakingPrecedesTheAuthoredCounterRemovalEvenInsideNestedCosts(string counter, bool nested)
    {
        // Taking is unpaid "unless all of that damage was taken". Engine choice:
        // taking precedes other physical costs, regardless of authored nesting.
        string cost = """
            {"removeCounters":{"card":"this","counter":"COUNTER","count":1}},
            {"heal":{"card":"this","amount":2}},{"takeDamage":{"cards":"this","amount":2}}
            """.Replace("COUNTER", counter, StringComparison.Ordinal);
        if (nested) cost = "{\"seq\":[" + cost + "]}";
        const string protection = """
            ,{"trigger":{"timing":"Constant","subject":"this"},
              "effect":{"preventDamageWhile":{"card":"this","condition":{
                "not":{"atLeast":{"value":{"countersOn":{"card":"this","counter":"test"}},"count":1}}}}}}
            """;
        var probed = Board(cost, protection);
        var control = Board(cost, protection);
        string before = Snapshot(probed.World);
        for (int i = 0; i < 3; i++)
            Assert.Single(probed.Runner.Actions(probed.World, 0), action => action.Card == probed.Source.ObjectId);
        Assert.Equal(before, Snapshot(probed.World));
        Execute(probed);
        Execute(control);
        Assert.Equal(0, probed.Source.Damage);
        Assert.Equal(0, probed.Source.Tokens.GetValueOrDefault("c_test"));
        Assert.Single(probed.World.Seats[0].Hand.Cards);
        Assert.Empty(probed.World.Agenda.Outstanding);
        Assert.Equal(Snapshot(control.World), Snapshot(probed.World));
    }

    [Fact]
    [Rule("rr:uses-x-type.1")]
    public void LastUsesCounterAfterAllHealthCostsRetainsItsLiveDeparture()
    {
        // "If there are no all-purpose counters on this card, discard this card."
        const string cost = """
            {"dealDamage":{"cards":"you","amount":2}},{"heal":{"card":"you","amount":2}},
            {"removeCounters":{"card":"this","counter":"test","count":1}}
            """;
        var board = Board(cost, "", uses: true);
        string before = Snapshot(board.World);
        Assert.Single(board.Runner.Actions(board.World, 0), action => action.Card == board.Source.ObjectId);
        Assert.Equal(before, Snapshot(board.World));
        Execute(board);
        Assert.Equal(0, board.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, board.Source.Area.Type);
        Assert.Single(board.World.Seats[0].Hand.Cards);
        Assert.Empty(board.World.Agenda.Outstanding);
    }

    private static void Execute((World World, AbilityRunner Runner, Card Source) board)
    {
        var action = Assert.Single(board.Runner.Actions(board.World, 0), option => option.Card == board.Source.ObjectId);
        board.Runner.ExecuteAction(board.World, new PlayerAction(action, [], []));
    }

    private static (World World, AbilityRunner Runner, Card Source) Board(string cost, string other, bool uses = false)
    {
        string pool = uses ? "\"startingCounters\":{\"type\":\"test\",\"count\":1,\"uses\":true}," : "";
        string json = """
            {"cards":[{"card":"01066",POOL"abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"seq":[COST]},"effect":{"draw":{"player":"you","count":1}}}OTHER]}]}
            """.Replace("POOL", pool, StringComparison.Ordinal)
            .Replace("COST", cost, StringComparison.Ordinal).Replace("OTHER", other, StringComparison.Ordinal);
        var runner = new AbilityRunner(AbilityCatalog.Parse(json));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01066", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        source.PlaceTokens("c_test", 1);
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("01088", world.Seats[0].Deck);
        return (world, runner, source);
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage,
        Pending = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject, step.Seat }),
    });
}
