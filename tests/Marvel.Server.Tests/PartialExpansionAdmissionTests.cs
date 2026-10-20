using System.Text.Json.Nodes;
using Marvel.Protocol;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Incomplete expansion data remains research material across every runtime entry point.</summary>
public sealed class PartialExpansionAdmissionTests
{
    [Fact]
    public void PartialExpansionDoesNotLeakThroughOrdinaryDeckOrSetupCatalogs()
    {
        WithPartialDataset(root =>
        {
            var factory = DatasetGameFactory.Load(root);
            Assert.Null(factory.CampaignDefinition);
            Assert.DoesNotContain(factory.DiscoverSetup().Heroes, hero => hero.Key == "hawkeye");
            Assert.DoesNotContain(factory.DiscoverSetup().Scenarios, scenario => scenario.Key == "crossbones");
            Assert.DoesNotContain(factory.DiscoverDecks().Cards, card => card.Face == "04042");
            var hero = factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "spider_man");
            Assert.False(factory.AssessDeck(hero.Key, [.. hero.Signature, "04042"]).CanExtend);
            Assert.False(factory.DeckQuantities(hero.Key, hero.Signature).ContainsKey("04042"));
            var research = Enumerable.Range(0, 5).SelectMany(page => factory.BrowseCards(page).Cards);
            Assert.False(Assert.Single(research, card => card.Face == "04042").RuntimeSupported);
            Assert.NotNull(factory.Create(new GameSpecification("rhino", ["spider_man"], [], 7)));
            Assert.Throws<KeyNotFoundException>(() => factory.Create(
                new GameSpecification("crossbones", ["spider_man"], [], 7)));
        });
    }

    [Fact]
    public void UnavailableExpansionReferenceInCoreRecipeStillFailsAdmission()
    {
        WithPartialDataset(root =>
        {
            string path = Path.Combine(root, "datasets", "setup", "setup.json");
            var setup = JsonNode.Parse(File.ReadAllText(path))!;
            setup["heroes"]!["spider_man"]!["player_deck"]!.AsArray().Add("04042");
            File.WriteAllText(path, setup.ToJsonString());
            var error = Assert.Throws<ArgumentException>(() => DatasetGameFactory.Load(root));
            Assert.Contains("outside the admitted executable content", error.Message);
        });
    }

    private static void WithPartialDataset(Action<string> check)
    {
        string root = Path.Combine(Path.GetTempPath(), "marvel-partial-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (string dataset in new[] { "setup", "cards", "abilities" })
            {
                string target = Path.Combine(root, "datasets", dataset);
                Directory.CreateDirectory(target);
                foreach (string source in Directory.GetFiles(RepositoryPaths.Dataset(dataset), "*.json"))
                    File.Copy(source, Path.Combine(target, Path.GetFileName(source)));
            }
            string setupPath = Path.Combine(root, "datasets", "setup", "setup.json");
            var setup = JsonNode.Parse(File.ReadAllText(setupPath))!;
            var hero = setup["heroes"]!["spider_man"]!.DeepClone();
            hero["hero"] = new JsonArray("04001a,04001b");
            setup["heroes"]!["hawkeye"] = hero;
            var scenario = setup["campaigns"]!["rhino"]!.DeepClone();
            scenario["villain"] = new JsonArray("04058", "04059");
            setup["campaigns"]!["crossbones"] = scenario;
            File.WriteAllText(setupPath, setup.ToJsonString());
            // Force an incomplete product even when the real dataset becomes complete.
            string abilityPath = Path.Combine(root, "datasets", "abilities", "abilities.json");
            var abilities = JsonNode.Parse(File.ReadAllText(abilityPath))!;
            var authored = abilities["cards"]!.AsArray();
            foreach (var card in authored.Where(card => card!["card"]!.GetValue<string>() == "04001a").ToArray())
                authored.Remove(card);
            File.WriteAllText(abilityPath, abilities.ToJsonString());
            check(root);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
