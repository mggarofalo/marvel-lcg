using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic grouping controls for ordinary payable costs and exact resource answers.</summary>
public sealed class IndependentNestedCostReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cost.5")]
    [Rule("rr:initiating-abilities.step.5")]
    public void AvailableResourcesDoNotValidateAnInsufficientSelectedPayment(bool nested)
    {
        // Costs "must be paid simultaneously"; inability to pay must "abort
        // this process without paying any costs". The full pool is available.
        var (world, runner, source) = Board(nested,
            """{"spendAny":1}""", """{"spendAny":1}""");
        var first = world.CreateCard("01091", world.Seats[0].Hand);
        var second = world.CreateCard("01091", world.Seats[0].Hand);
        var offer = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        string before = Snapshot(world);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(offer, [first.ObjectId], [])));
        Assert.Equal(before, Snapshot(world));
        runner.ExecuteAction(world, new PlayerAction(offer, [first.ObjectId, second.ObjectId], []));
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.Single(world.Seats[0].Hand.Cards);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:cost.5")]
    public void AlreadyPayableDamageAndHealingNeedNoFutureDamageProjection(bool nested, bool healFirst)
    {
        // Multiple costs "must be paid simultaneously". Two existing damage
        // already pays the healing component, independent of generator projection.
        const string damage = """{"dealDamage":{"cards":"you","amount":1}}""";
        const string heal = """{"heal":{"card":"you","amount":2}}""";
        var (world, runner, source) = Board(nested, healFirst ? heal : damage, healFirst ? damage : heal);
        world.Seats[0].IdentityCard.TakeDamage(2);
        string before = Snapshot(world);
        var offer = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        runner.ExecuteAction(world, new PlayerAction(offer, [], []));
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static (World World, AbilityRunner Runner, Card Source) Board(bool nested, params string[] components)
    {
        string cost = "{\"seq\":[" + string.Join(",", components.Select(component =>
            nested ? "{\"seq\":[" + component + "]}" : component)) + "]}";
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{{{cost}}},"effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01006",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
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
