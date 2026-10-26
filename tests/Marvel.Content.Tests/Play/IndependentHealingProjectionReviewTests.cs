using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic ordered counter and health costs; no content admission.</summary>
public sealed class IndependentHealingProjectionReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [Rule("rr:ability.9")]
    [Rule("rr:initiating-abilities.step.5")]
    public void EarlierCounterPaymentChangesDamageProtection(int counters, bool offered)
    {
        // Constants are active "anytime the specific condition is met".
        // Removing the last counter prohibits the damage needed by the heal.
        var (world, runner, source) = Board(counters);
        string before = Snapshot(world);
        Assert.Equal(offered, runner.Actions(world, 0).Any(action => action.Card == source.ObjectId));
        Assert.Equal(before, Snapshot(world));
        if (!offered) return;
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(0, source.Damage);
        Assert.Equal(1, source.Tokens.GetValueOrDefault("c_test"));
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void SelectedUnpayableHealingPreservesTheEarlierCounterCost()
    {
        // If costs cannot be paid, "abort this process without paying any costs".
        var (world, runner, source) = Board(1);
        string before = Snapshot(world);
        var forged = new PendingAbility(source.ObjectId, AbilityType.Action, 0, 0);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(forged, [], [])));
        Assert.Equal(before, Snapshot(world));
    }

    private static (World World, AbilityRunner Runner, Card Source) Board(int counters)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01066","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"seq":[{"removeCounters":{"card":"this","counter":"test","count":1}},
                {"dealDamage":{"cards":"this","amount":2}},{"heal":{"card":"this","amount":2}}]},
              "effect":{"draw":{"player":"you","count":1}}
            },{"trigger":{"timing":"Constant","subject":"this"},
              "effect":{"preventDamageWhile":{"card":"this","condition":{
                "not":{"atLeast":{"value":{"countersOn":{"card":"this","counter":"test"}},"count":1}}}}}
            }]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01066", world.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(0), cardOwner: 0));
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
