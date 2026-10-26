using System.Text.Json.Nodes;
using Marvel.Protocol;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class RuntimeProductAdmissionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredUnacceptedProductCannotEnterDiscoveryDecksOrGameCreation(bool unknownProduct)
    {
        WithResearchDataset(unknownProduct, root =>
        {
            var factory = DatasetGameFactory.Load(root);
            Assert.NotNull(factory.CampaignDefinition);
            Assert.Contains(factory.DiscoverSetup().Heroes, hero => hero.Key == "hawkeye");
            Assert.DoesNotContain(factory.DiscoverSetup().Heroes, hero => hero.Key == "research_hero");
            Assert.DoesNotContain(factory.DiscoverSetup().Scenarios, scenario => scenario.Key == "research_scenario");
            Assert.DoesNotContain(factory.DiscoverDecks().Cards, card => card.Face is "08003" or "08024");
            var hero = factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "spider_man");
            string[] draft = [.. hero.Starter, "08024"];
            Assert.False(factory.AssessDeck(hero.Key, draft).CanExtend);
            Assert.False(factory.AssessCampaignDeck(hero.Key, draft, []).CanExtend);
            Assert.False(factory.DeckQuantities(hero.Key, hero.Signature).ContainsKey("08024"));
            Assert.False(factory.CampaignDeckQuantities(hero.Key, hero.Signature, []).ContainsKey("08024"));
            var research = Enumerable.Range(0, 5).SelectMany(page => factory.BrowseCards(page).Cards);
            Assert.False(Assert.Single(research, card => card.Face == "08024").RuntimeSupported);
            Assert.Throws<ArgumentException>(() => factory.Create(
                new GameSpecification("rhino", [hero.Key], [], 7, [new(draft)])));
            var store = new MemorySessionStore();
            var rejected = new EngineHost(factory, store: store).Exchange(EngineRequest.OpenGame(
                "research-deck", "research-game", new GameSpecification("rhino", [hero.Key], [], 7, [new(draft)])));
            Assert.NotNull(rejected.Error);
            Assert.Empty(store.Load());
            Assert.Throws<KeyNotFoundException>(() => factory.Create(
                new GameSpecification("rhino", ["research_hero"], [], 7)));
            Assert.Throws<KeyNotFoundException>(() => factory.Create(
                new GameSpecification("research_scenario", [hero.Key], [], 7)));
            Assert.NotNull(factory.Create(new GameSpecification("rhino", [hero.Key], [], 7)));
            Assert.NotNull(factory.Create(new GameSpecification("crossbones", ["hawkeye"], [], 7)));
        });
    }

    [Fact]
    public void CoreRecipeCannotSmuggleAnAuthoredResearchCardAcrossTheBoundary()
    {
        WithResearchDataset(false, root =>
        {
            string path = Path.Combine(root, "datasets", "setup", "setup.json");
            var setup = JsonNode.Parse(File.ReadAllText(path))!;
            setup["heroes"]!["spider_man"]!["player_deck"]!.AsArray().Add("08024");
            File.WriteAllText(path, setup.ToJsonString());
            var error = Assert.Throws<ArgumentException>(() => DatasetGameFactory.Load(root));
            Assert.Contains("outside the admitted executable content", error.Message);
        });
    }

    private static void WithResearchDataset(bool unknownProduct, Action<string> check)
    {
        string root = Path.Combine(Path.GetTempPath(), "marvel-research-" + Guid.NewGuid().ToString("N"));
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
            // Synthetic ownership recipes, not a claim about the pack's starter or scenario.
            var hero = setup["heroes"]!["spider_man"]!.DeepClone();
            hero["hero"] = new JsonArray("08001a,08001b");
            setup["heroes"]!["research_hero"] = hero;
            var scenario = setup["campaigns"]!["rhino"]!.DeepClone();
            scenario["villain"] = new JsonArray("08028");
            setup["campaigns"]!["research_scenario"] = scenario;
            File.WriteAllText(setupPath, setup.ToJsonString());
            string abilityPath = Path.Combine(root, "datasets", "abilities", "abilities.json");
            var abilities = JsonNode.Parse(File.ReadAllText(abilityPath))!;
            var authored = abilities["cards"]!.AsArray();
            foreach (string face in new[] { "08001a", "08001b", "08003", "08024", "08028" })
                if (!authored.Any(card => card!["card"]!.GetValue<string>() == face))
                    authored.Add(new JsonObject { ["card"] = face, ["abilities"] = new JsonArray() });
            File.WriteAllText(abilityPath, abilities.ToJsonString());
            if (unknownProduct)
            {
                string cardPath = Path.Combine(root, "datasets", "cards", "cards.json");
                var cards = JsonNode.Parse(File.ReadAllText(cardPath))!;
                foreach (var card in cards["cards"]!.AsArray().Where(card => card!["pack"]!.GetValue<string>() == "bkw"))
                    card!["pack"] = "unreviewed-product";
                File.WriteAllText(cardPath, cards.ToJsonString());
            }
            check(root);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
