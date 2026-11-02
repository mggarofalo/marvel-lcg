using Marvel.Protocol;
using Marvel.Server;
using Marvel.Sim;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class SimulationProductionSetupTests
{
    [Fact]
    public void EverySimulatedScenarioOpensTheBoardServerCreationOpens()
    {
        // The simulator is a research host over the product. A board dealt any other
        // way than server creation would report failures no player can reach.
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var data = SimulationData.Load(RepositoryPaths.Root);
        string[] heroes = ["spider_man", "captain_marvel"];
        var opened = new List<string>();
        foreach (var choice in factory.DiscoverSetup().Scenarios)
        {
            var config = Config(choice.Key, choice.Expert, heroes);
            if (SimulationHarnessSupport.CampaignKey(config) != choice.Key) continue;
            var simulated = Marvel.Sim.OpenedGame.Open(data, config, 8123).Game;
            var created = factory.Create(new GameSpecification(choice.Key, heroes, null, 8123)).Game;
            Assert.Equal(created.State.Digest().Canonical(), simulated.State.Digest().Canonical());
            Assert.Equal(created.State.Random.Generator.WordsConsumed, simulated.State.Random.Generator.WordsConsumed);
            Assert.Equal(created.State.Expert, simulated.State.Expert);
            Assert.Equal((created.Pending?.Player, created.Pending?.Label), (simulated.Pending?.Player, simulated.Pending?.Label));
            opened.Add(choice.Key);
        }
        Assert.Contains("wrecking_crew_standard", opened);
        Assert.Contains("zola", opened);
    }

    private static SimulationConfig Config(string key, bool expert, IReadOnlyList<string> heroes) => expert
        ? new(key.EndsWith("_expert", StringComparison.Ordinal) ? key[..^"_expert".Length] : key,
            "expert", heroes, null, 1, [8123], null, null, 1, 600, null, RepositoryPaths.Root)
        : new(key, "standard", heroes, null, 1, [8123], null, null, 1, 600, null, RepositoryPaths.Root);
}
