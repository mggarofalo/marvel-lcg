using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic payment dependency audit, without content admission.</summary>
public sealed class HealingCounterDependencyTests : CoreCardTestBase
{
    [Theory]
    [InlineData("test", 2, false)]
    [InlineData("test", 3, true)]
    [InlineData("allPurpose", 2, false)]
    [InlineData("allPurpose", 3, true)]
    [Rule("rr:ability.9")]
    [Rule("rr:initiating-abilities.step.5")]
    public void RepeatedCounterPaymentsShareEvolvingQuantities(string counter, int count, bool offered)
    {
        // Constants are active "anytime the specific condition is met".
        string removal = """{"removeCounters":{"card":"this","counter":"COUNTER","count":1}}"""
            .Replace("COUNTER", counter, StringComparison.Ordinal);
        string cycle = removal + """,{"dealDamage":{"cards":"this","amount":2}},{"heal":{"card":"this","amount":2}}""";
        var (world, runner, source) = Board(cycle + "," + cycle, Protection, count);
        string before = Snapshot(world);
        Assert.Equal(offered, runner.Actions(world, 0).Any(action => action.Card == source.ObjectId));
        Assert.Equal(before, Snapshot(world));
        var action = new PendingAbility(source.ObjectId, AbilityType.Action, 0, 0);
        if (!offered)
        {
            Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [], [])));
            Assert.Equal(before, Snapshot(world));
            return;
        }
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(1, source.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(0, source.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void CounterPaymentCanEnableAnUnsupportedForcedReplacement()
    {
        // If costs cannot be paid, "abort this process without paying any costs".
        const string replacement = """
            {"trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","subject":"this"},
             "when":{"not":{"atLeast":{"value":{"countersOn":{"card":"this","counter":"test"}},"count":1}}},
             "effect":{"draw":{"player":"you","count":1}}}
            """;
        var (world, runner, source) = Board(CounterHealth, replacement, 1);
        string before = Snapshot(world);
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Contains("unknown damage replacement", error.Message, StringComparison.Ordinal);
        Assert.Equal(before, Snapshot(world));
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 0, 0), [], [])));
        Assert.Equal(before, Snapshot(world));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:uses-x-type.1")]
    [Rule("rr:initiating-abilities.step.5")]
    public void LastUsesDepartureHasAnExplicitBoundaryWithALegalCounterControl(int count)
    {
        // "If there are no all-purpose counters on this card, discard this card."
        string cost = CounterHealth.Replace("\"cards\":\"this\"", "\"cards\":\"you\"", StringComparison.Ordinal)
            .Replace("\"heal\":{\"card\":\"this\"", "\"heal\":{\"card\":\"you\"", StringComparison.Ordinal);
        var (world, runner, source) = Board(cost, null, count, uses: true);
        string before = Snapshot(world);
        if (count == 1)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
            Assert.Contains("last Uses counter departure", error.Message, StringComparison.Ordinal);
            Assert.Equal(before, Snapshot(world));
            Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
                new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 0, 0), [], [])));
            Assert.Equal(before, Snapshot(world));
            return;
        }
        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(1, source.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:initiating-abilities.step.5")]
    public void NativeCounterTransitionIsExplicitAndNonTransitioningRemovalRemainsLegal(int count)
    {
        // If costs cannot be paid, "abort this process without paying any costs".
        // The native transition may execute When Revealed work; the health trace cannot simulate it.
        const string cost = """
            {"removeCounters":{"card":"this","counter":"infamy","count":1}},
            {"dealDamage":{"cards":"you","amount":2}},{"heal":{"card":"you","amount":2}}
            """;
        var (world, runner, source) = Board(cost, null, 0, face: "02006a");
        source.PlaceTokens("c_infamy", count);
        world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
        string before = Snapshot(world);
        if (count == 1)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
            Assert.Contains("native counter transitions", error.Message, StringComparison.Ordinal);
            Assert.Equal(before, Snapshot(world));
            Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
                new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 0, 0), [], [])));
            Assert.Equal(before, Snapshot(world));
            return;
        }
        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal("02006a", source.FaceId);
        Assert.Equal(1, source.Tokens.GetValueOrDefault("c_infamy"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:exhausted.2")]
    public void EarlierExhaustionMakesAReplacementExhaustCostUnpayable(bool titled)
    {
        // "An exhausted card cannot be exhausted again until it is ready."
        const string replacement = """
            {"trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","subject":"this"},
             "cost":{"exhaust":"this"},"effect":{"draw":{"player":"you","count":1}}}
            """;
        string exhaust = titled ? """{"exhaustTitled":"Hawkeye"}""" : """{"exhaust":"this"}""";
        var (world, runner, source) = Board(exhaust + "," + CounterHealth, replacement, 1);
        string before = Snapshot(world);
        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.False(source.Ready);
        Assert.Equal(0, source.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void DirectReturnBeforeIdentityDamageAndHealingRemainsPayable()
    {
        // "Pay the cost(s)": source departure alone does not prohibit the remaining identity costs.
        const string cost = """
            {"returnToHand":"this"},{"dealDamage":{"cards":"you","amount":2}},
            {"heal":{"card":"you","amount":2}}
            """;
        var (world, runner, source) = Board(cost, null, 0);
        string before = Snapshot(world);
        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private const string CounterHealth = """
        {"removeCounters":{"card":"this","counter":"test","count":1}},
        {"dealDamage":{"cards":"this","amount":2}},{"heal":{"card":"this","amount":2}}
        """;

    private const string Protection = """
        {"trigger":{"timing":"Constant","subject":"this"},"effect":{"preventDamageWhile":{"card":"this",
          "condition":{"not":{"atLeast":{"value":{"countersOn":{"card":"this","counter":"test"}},"count":1}}}}}}
        """;

    private static (World World, AbilityRunner Runner, Card Source) Board(
        string cost, string? other, int counters, bool uses = false, string face = "01066")
    {
        string pool = uses ? """
            "startingCounters":{"type":"test","count":2,"uses":true},
            """ : "";
        string json = """
            {"cards":[{"card":"FACE",POOL"abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "anyPlayer":true,"cost":{"seq":[COST]},"effect":{"draw":{"player":"you","count":1}}}OTHER]}]}
            """.Replace("FACE", face, StringComparison.Ordinal)
            .Replace("POOL", pool, StringComparison.Ordinal)
            .Replace("COST", cost, StringComparison.Ordinal)
            .Replace("OTHER", other is null ? "" : "," + other, StringComparison.Ordinal);
        var runner = new AbilityRunner(AbilityCatalog.Parse(json));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard(face == "02006a" ? "02006a,02006b" : face, world.AreaOf(
            face == "01066" ? DeckType.AlliesArea : DeckType.EnvironmentArea, PlayArea.Of(0), cardOwner: 0));
        source.PlaceTokens("c_test", counters);
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
