using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class IndependentFullHealingCostTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cost.5")]
    [Rule("rr:initiating-abilities.step.5")]
    public void SeparateHealingComponentsShareOneDamagePool(bool nested)
    {
        // Costs "must be paid simultaneously"; if they cannot, "abort this
        // process without paying any costs". These are synthetic DSL bodies.
        const string heal = """{"heal":{"card":"you","amount":1}}""";
        string component = nested ? "{\"seq\":[" + heal + "]}" : heal;
        string book = $$$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"seq":[{{{component}}},{{{component}}}]},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """;
        var runner = new AbilityRunner(AbilityCatalog.Parse(book));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("01006",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01088", world.Seats[0].Deck);
        world.Seats[0].IdentityCard.TakeDamage(1);
        string before = Snapshot(world);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, Snapshot(world));
        world.Seats[0].IdentityCard.TakeDamage(1);
        var offered = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(offered, [], []));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.Seats[0].Hand.Cards);
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage,
        Pending = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject, step.Seat }),
    });
}
