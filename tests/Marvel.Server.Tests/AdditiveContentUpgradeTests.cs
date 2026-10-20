using System.Text.Json.Nodes;
using Marvel.Protocol;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class AdditiveContentUpgradeTests
{
    [Fact]
    public void KnownAdditiveContentRequiresExactOriginalTraceAndPreservesPendingPrompt()
    {
        WithAddedScenario((root, original) =>
        {
            var store = new MemorySessionStore();
            var host = new EngineHost(original, store: store);
            var opened = host.Exchange(EngineRequest.OpenGame("old-open", "old-core",
                new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
            Assert.Null(opened.Error);
            var saved = Assert.Single(store.Load()).Save;
            string before = SessionSaveJson.Write(saved);
            var upgraded = DatasetGameFactory.Load(root);
            var route = Assert.IsType<SessionContentUpgrade>(upgraded.ContentUpgrade(saved.Compatibility));
            var candidate = route.Verify(saved, setup => Open(upgraded, setup));
            Assert.Equal(upgraded.Compatibility, candidate.Compatibility);
            Assert.Equal(before, SessionSaveJson.Write(candidate with { Compatibility = saved.Compatibility }));
            var game = SessionReplay.Verify(candidate, upgraded.Compatibility, setup => Open(upgraded, setup));
            Assert.Equal(saved.Initial.StateDigest, game.State.Digest().Canonical());
            Assert.Equal(before, SessionSaveJson.Write(saved));
            var unknown = saved.Compatibility with { CardsSha256 = new string('a', 64) };
            Assert.Null(upgraded.ContentUpgrade(unknown));
        });
    }

    [Theory]
    [InlineData("cards")]
    [InlineData("engine-card")]
    [InlineData("abilities")]
    [InlineData("setup")]
    public void ChangedAdmittedEntryHasNoUpgradeEvenWhenTheSavedTraceDidNotReachIt(string category)
    {
        WithAddedScenario((root, original) =>
        {
            string dataset = category == "engine-card" ? "cards" : category;
            string path = Path.Combine(root, "datasets", dataset, dataset + ".json");
            var data = JsonNode.Parse(File.ReadAllText(path))!;
            if (category == "cards")
                data["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == "01001a")!["name"] = "Altered identity";
            else if (category == "engine-card")
                data["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == "rule_a")!["name"] = "Altered rule source";
            else if (category == "abilities")
                data["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "01001a")!
                    ["abilities"]![0]!["effect"]!["draw"]!["count"] = 2;
            else
                data["campaigns"]!["rhino"]!["name"] = "Altered scenario";
            File.WriteAllText(path, data.ToJsonString());
            Assert.Null(DatasetGameFactory.Load(root).ContentUpgrade(original.Compatibility));
        });
    }

    private static ReplayOpenedGame Open(DatasetGameFactory factory, SessionSetup setup)
    {
        var game = factory.Create(new GameSpecification(setup.Scenario, setup.Heroes,
            setup.ModularSets, setup.Seed, setup.PlayerDecks) { Campaign = setup.Campaign });
        return new(game.Game, game.SetupEvents);
    }

    [Fact]
    public void StrictReplayRejectsAnUnrelatedAddedSetupEvenForAnUnchangedCoreGame()
    {
        WithAddedScenario((root, original) =>
        {
            var store = new MemorySessionStore();
            var host = new EngineHost(original, store: store);
            var opened = host.Exchange(EngineRequest.OpenGame("old-open", "old-core",
                new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
            Assert.Null(opened.Error);
            var saved = Assert.Single(store.Load()).Save;
            Assert.NotNull(saved.CurrentPrompt);
            byte[] before = System.Text.Encoding.UTF8.GetBytes(SessionSaveJson.Write(saved));
            var upgraded = DatasetGameFactory.Load(root);
            Assert.NotEqual(original.Compatibility.SetupSha256, upgraded.Compatibility.SetupSha256);
            var mismatch = Assert.Throws<SessionCompatibilityException>(() =>
                SessionReplay.Verify(saved, upgraded.Compatibility, setup =>
                {
                    var game = upgraded.Create(new GameSpecification(setup.Scenario, setup.Heroes,
                        setup.ModularSets, setup.Seed));
                    return new ReplayOpenedGame(game.Game, game.SetupEvents);
                }));
            Assert.Equal("setup_dataset_mismatch", mismatch.Category);
            Assert.Equal(before, System.Text.Encoding.UTF8.GetBytes(SessionSaveJson.Write(saved)));
        });
    }

    internal static void WithAddedScenario(Action<string, DatasetGameFactory> check)
    {
        string root = Path.Combine(Path.GetTempPath(), "marvel-content-upgrade-" + Guid.NewGuid().ToString("N"));
        try
        {
            string archive = Path.Combine(RepositoryPaths.Root, "tests", "Marvel.Server.Tests",
                "Fixtures", "content-91098022.zip");
            Assert.Equal("7184323687CE9DFB5CAB49AA00447A9D5233D2DF3EC24F7EF58B5C5187C9F880",
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(archive))));
            string predecessor = Path.Combine(root, "predecessor");
            System.IO.Compression.ZipFile.ExtractToDirectory(archive, predecessor);
            var original = DatasetGameFactory.Load(predecessor);
            foreach (string dataset in new[] { "setup", "cards", "abilities" })
            {
                string target = Path.Combine(root, "datasets", dataset);
                Directory.CreateDirectory(target);
                foreach (string source in Directory.GetFiles(Path.Combine(predecessor, "datasets", dataset), "*.json"))
                    File.Copy(source, Path.Combine(target, Path.GetFileName(source)));
            }
            string path = Path.Combine(root, "datasets", "setup", "setup.json");
            var setup = JsonNode.Parse(File.ReadAllText(path))!;
            setup["campaigns"]!["upgrade_probe_rhino"] = setup["campaigns"]!["rhino"]!.DeepClone();
            File.WriteAllText(path, setup.ToJsonString());
            check(root, original);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }
}
