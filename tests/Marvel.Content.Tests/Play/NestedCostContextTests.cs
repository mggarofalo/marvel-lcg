using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic sequence shapes share one resource, counter and damage context.</summary>
public sealed class NestedCostContextTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:cost.5")]
    [Rule("rr:initiating-abilities.step.5")]
    public void SiblingComponentsCannotReuseOneResourceOrCounter(bool nested, bool counters)
    {
        // Costs "must be paid simultaneously"; inability to pay must "abort
        // this process without paying any costs". Grouping cannot multiply a pool.
        string component = counters
            ? """{"removeCounters":{"card":"this","counter":"test","count":1}}"""
            : """{"spendAny":1}""";
        var runner = RunnerWithCost(Sequence(nested, component, component));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01006",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("01088", world.Seats[0].Deck);
        var paying = new List<int>();
        void AddPayment()
        {
            if (counters) source.PlaceTokens("c_test", 1);
            else paying.Add(world.CreateCard("01091", world.Seats[0].Hand).ObjectId);
        }
        AddPayment();
        int held = world.Seats[0].Hand.Cards.Count;
        string before = Snapshot(world);
        for (int query = 0; query < 2; query++)
            Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        var forged = new PendingAbility(source.ObjectId, AbilityType.Action, 0, 0);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(forged, paying, [])));
        Assert.Equal(before, Snapshot(world));
        AddPayment();
        var offered = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(offered, paying, []));
        Assert.Equal(counters ? held + 1 : 1, world.Seats[0].Hand.Cards.Count);
        if (counters) Assert.Equal(0, source.Tokens.GetValueOrDefault("c_test"));
        else Assert.All(paying, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DirectDamageCanSupplyTheFullHealingCost(bool nested)
    {
        var runner = RunnerWithCost(Sequence(nested,
            """{"dealDamage":{"cards":"you","amount":1}}""",
            """{"heal":{"card":"you","amount":2}}"""));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01006",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("01088", world.Seats[0].Deck);
        world.Seats[0].IdentityCard.TakeDamage(1);
        string before = Snapshot(world);
        var offer = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        runner.ExecuteAction(world, new PlayerAction(offer, [], []));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
    }

    private static string Sequence(bool nested, params string[] components) =>
        "{\"seq\":[" + string.Join(",", components.Select(component =>
            nested ? "{\"seq\":[" + component + "]}" : component)) + "]}";

    private static AbilityRunner RunnerWithCost(string cost) => new(AbilityCatalog.Parse($$$"""
        {"cards":[{"card":"01006","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
          "cost":{{{cost}}},"effect":{"draw":{"player":"you","count":1}}
        }]}]}
        """));

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage,
        Pending = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject, step.Seat }),
    });
}
