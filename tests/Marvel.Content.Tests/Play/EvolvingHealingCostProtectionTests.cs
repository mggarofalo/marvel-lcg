using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic evolving cost protection, without additional content admission.</summary>
public sealed class EvolvingHealingCostProtectionTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:ability.9")]
    [Rule("rr:initiating-abilities.step.5")]
    public void HealingDeactivatesProtectionBeforeTheNextDamage(bool remainingHealth, bool explicitTarget)
    {
        // Conditional constants are active "anytime the specific condition is met".
        // The first hit enables protection, then the intervening heal disables it.
        string condition = remainingHealth
            ? """{"not":{"atLeast":{"value":{"remainingHealth":"this"},"count":2}}}"""
            : """{"atLeast":{"value":{"damageOn":"this"},"count":2}}""";
        string protection = """{"trigger":{"timing":"Constant","subject":"this"},"effect":{"preventDamageWhile":{TARGET"condition":CONDITION}}}"""
            .Replace("TARGET", explicitTarget ? "\"card\":{\"titled\":\"Daredevil\"}," : "\"card\":\"this\",", StringComparison.Ordinal)
            .Replace("CONDITION", condition, StringComparison.Ordinal);
        var (world, runner, source) = Board(protection, true);
        string before = Snapshot(world);
        for (int i = 0; i < 2; i++)
            Assert.Contains(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        var action = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(0, source.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void ReplacementEnabledByEarlierDamageRaisesBeforePayment()
    {
        // If costs cannot be paid, "abort this process without paying any costs".
        // The forced replacement becomes eligible for the second cost instance;
        // its arbitrary effect has no supported damage projection.
        const string replacement = """
            {"trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","subject":"this"},
             "when":{"atLeast":{"value":{"damageOn":"this"},"count":1}},
             "effect":{"draw":{"player":"you","count":1}}}
            """;
        var (world, runner, source) = Board(replacement, false);
        string before = Snapshot(world);
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Contains("unknown damage replacement", error.Message, StringComparison.Ordinal);
        Assert.Equal(before, Snapshot(world));
        var forged = new PendingAbility(source.ObjectId, AbilityType.Action, 0, 0);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(forged, [], [])));
        Assert.Equal(before, Snapshot(world));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(3, true)]
    [Rule("rr:ability.9")]
    [Rule("rr:initiating-abilities.step.5")]
    public void DamageToAnotherCostTargetChangesProtection(int threshold, bool offered)
    {
        // Constants are active "anytime the specific condition is met"; the
        // identity's earlier damage controls the ally's later prohibition.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01066","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"seq":[{"dealDamage":{"cards":"you","amount":2}},
                {"dealDamage":{"cards":"this","amount":2}},{"heal":{"card":"this","amount":1}}]},
              "effect":{"draw":{"player":"you","count":1}}
            },{"trigger":{"timing":"Constant","subject":"this"},
              "effect":{"preventDamageWhile":{"card":"this","condition":{
                "atLeast":{"value":{"damageOn":"you"},"count":THRESHOLD}}}}}]}]}
            """.Replace("THRESHOLD", threshold.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01066", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("01088", world.Seats[0].Deck);
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
        Assert.Equal(1, source.Damage);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void ExactSelectedGeneratorCanEnableProtectionEvenWhenHandPaymentWorks()
    {
        // An unpayable selected cost must "abort this process without paying any costs".
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01066","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"seq":[{"spend":"Y"},{"dealDamage":{"cards":"this","amount":2}},
                {"heal":{"card":"this","amount":2}}]},"effect":{"draw":{"player":"you","count":1}}
            },{"trigger":{"timing":"Constant","subject":"this"},
              "effect":{"preventDamageWhile":{"card":"this","condition":{
                "atLeast":{"value":{"damageOn":"you"},"count":1}}}}}]},
            {"card":"01091","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"this"},
              "cost":{"dealDamage":{"cards":"you","amount":1}},"effect":{"generate":"Y"}}]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01066", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var generator = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var hand = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("01088", world.Seats[0].Deck);
        string before = Snapshot(world);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [generator.ObjectId], [])));
        Assert.Equal(before, Snapshot(world));
        runner.ExecuteAction(world, new PlayerAction(action, [hand.ObjectId], []));
        Assert.Equal(0, source.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void AlreadyPayableHealingAcceptsMultipleHarmlessGenerators()
    {
        // "Pay the cost(s)"; selected exhaustion-only generators do not change
        // the damage funding this already-payable healing cost.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01066","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"seq":[{"spend":"YY"},{"heal":{"card":"this","amount":2}}]},
              "effect":{"draw":{"player":"you","count":1}}}]},
            {"card":"01091","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"this"},
              "cost":{"exhaust":"this"},"effect":{"generate":"Y"}}]},
            {"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"this"},
              "cost":{"exhaust":"this"},"effect":{"generate":"Y"}}]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01066", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        source.TakeDamage(2);
        var first = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var second = world.CreateCard("01006", first.Area);
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("01088", world.Seats[0].Deck);
        string before = Snapshot(world);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        runner.ExecuteAction(world, new PlayerAction(action, [first.ObjectId, second.ObjectId], []));
        Assert.Equal(0, source.Damage);
        Assert.False(first.Ready);
        Assert.False(second.Ready);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static (World World, AbilityRunner Runner, Card Source) Board(string extra, bool interveningHeal)
    {
        const string hit = """{"dealDamage":{"cards":"this","amount":2}}""";
        string steps = interveningHeal
            ? hit + "," + """{"heal":{"card":"this","amount":1}},""" + hit
            : hit + "," + hit;
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01066","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"seq":[STEPS,{"heal":{"card":"this","amount":3}}]},
              "effect":{"draw":{"player":"you","count":1}}
            },EXTRA]}]}
            """.Replace("STEPS", steps, StringComparison.Ordinal).Replace("EXTRA", extra, StringComparison.Ordinal)));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01066", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
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
