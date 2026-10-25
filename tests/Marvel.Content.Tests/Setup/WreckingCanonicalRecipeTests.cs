using Marvel.Content.Setup;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class WreckingCanonicalRecipeTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly string[] Heroes = ["spider_man", "captain_marvel", "she_hulk", "iron_man"];

    public static IEnumerable<object[]> Recipes() => Setup.CampaignNames
        .Where(key => key.StartsWith("wrecking_crew_", StringComparison.Ordinal))
        .SelectMany(key => Enumerable.Range(1, 4).Select(players => new object[] { key, players }));

    [Fact]
    public void CanonicalModesCoverAllSixteenCustomCombinations()
    {
        var keys = Setup.CampaignNames.Where(key => key.StartsWith("wrecking_crew_", StringComparison.Ordinal)).ToArray();
        Assert.Equal(19, keys.Length);
        var custom = keys.Where(key => key.Contains("_custom_", StringComparison.Ordinal)).ToArray();
        Assert.Equal(16, custom.Length);
        Assert.Equal(16, custom.Select(key => string.Join("", Setup.Campaign(key).EncounterGroups!.Groups.Select(group => group.Variant))).Distinct().Count());
        Assert.Equal("standard", Setup.Campaign("wrecking_crew_standard").SetupMode);
        Assert.Equal("expert", Setup.Campaign("wrecking_crew_expert").SetupMode);
        Assert.Equal("extreme", Setup.Campaign("wrecking_crew_extreme").SetupMode);
    }

    [Theory]
    [MemberData(nameof(Recipes))]
    public void CanonicalRecipeDealsEveryAuthoredModeAtEveryPlayerCount(string key, int players)
    {
        // pack:mc03:additional-setup: each villain has its own 15-card encounter deck;
        // obligations and nemesis cards are not used. Adjustable difficulty specifies A/B variants.
        var recipe = Setup.Campaign(key);
        var plan = ScenarioDealer.Prepare(Setup, new(key, Heroes.Take(players).ToArray()), Cards);
        var world = WorldSetup.DealConfigured(Cards, plan.Blueprints, Heroes.Take(players).ToArray(), 12345,
            new NoCardAbilities(), new(Expert: recipe.Expert, EncounterGroups: plan.EncounterGroups));
        var topology = Assert.IsType<EncounterTopology>(world.EncounterTopology);
        Assert.Equal(4, topology.Groups.Count);
        Assert.Same(topology.Groups[0], topology.Active);
        Assert.Empty(recipe.ModularSets);
        Assert.Empty(recipe.EncounterSets);
        Assert.DoesNotContain(plan.Blueprints, card => card.Slot is SetupSlot.Obligation or SetupSlot.Nemesis);
        for (int index = 0; index < 4; index++)
        {
            var group = topology.Groups[index];
            var authored = recipe.EncounterGroups!.Groups[index];
            Assert.Equal(WreckingRecipeFixture.Schemes[index], group.Scheme.FaceId);
            Assert.Equal(WreckingRecipeFixture.Decks[index].Order(), group.Deck.Cards.Select(card => card.FaceId).Order());
            string[] expectedStages = ExpectedStages(key, index);
            Assert.Equal(expectedStages, authored.Stages);
            Assert.Equal(expectedStages[0], group.CurrentVillain!.FaceId);
            Assert.Equal(key == "wrecking_crew_extreme" ? 2 : 1, authored.Stages.Count);
            Assert.Equal(key == "wrecking_crew_extreme" ? "A → B" : authored.Stages[0] == WreckingRecipeFixture.Stages[index][0] ? "A" : "B", authored.Variant);
        }
    }
    private static string[] ExpectedStages(string key, int index)
    {
        if (key == "wrecking_crew_extreme") return WreckingRecipeFixture.Stages[index];
        string variant = key == "wrecking_crew_expert" ? "bbbb"
            : key.Contains("_custom_", StringComparison.Ordinal) ? key[^4..] : "aaaa";
        return [WreckingRecipeFixture.Stages[index][variant[index] == 'b' ? 1 : 0]];
    }
}
