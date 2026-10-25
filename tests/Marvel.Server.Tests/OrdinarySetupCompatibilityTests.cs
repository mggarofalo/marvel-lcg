using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class OrdinarySetupCompatibilityTests
{
    [Fact]
    public void PublicCoreAndRiseOfRedSkullSetupRetainsOrdinaryAllocationAndRandomConsumption()
    {
        // Allocation and RNG consumption are engine wire choices, including the ordinary dealer order.
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var abilities = AbilityLowering.Book(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var scenarios = factory.DiscoverSetup().Scenarios.Where(choice => choice.Name != "Wrecking Crew").ToArray();
        Assert.NotEmpty(scenarios);
        foreach (var scenario in scenarios)
        {
            string[] heroes = ["spider_man", "captain_marvel"];
            var blueprints = Blueprints.From(Dealer.Deal(setup, new DealRequest(scenario.Key, heroes), cards), cards);
            var runner = new AbilityRunner(abilities);
            var legacy = Game.Begin(WorldSetup.Deal(cards, blueprints,
                heroes.Select(hero => setup.Hero(hero).Name).ToArray(), 8123, runner,
                expert: setup.Campaign(scenario.Key).Expert), cards, runner);
            var actual = factory.Create(new GameSpecification(scenario.Key, heroes, null, 8123)).Game;
            Assert.Null(actual.State.EncounterTopology);
            Assert.Equal(legacy.State.Digest().Canonical(), actual.State.Digest().Canonical());
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(legacy.State.Random.Generator.GetState()),
                System.Text.Json.JsonSerializer.Serialize(actual.State.Random.Generator.GetState()));
            Assert.Equal(legacy.State.Random.Generator.WordsConsumed, actual.State.Random.Generator.WordsConsumed);
        }
    }
}
