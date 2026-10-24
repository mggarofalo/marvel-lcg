using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Discovery contract only; component fixtures do not admit the scenario.</summary>
public sealed class ScenarioSetupProjectionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly string[][] Stages = [["07002", "07003"], ["07017", "07018"], ["07032", "07033"], ["07046", "07047"]];

    [Fact]
    public void EveryCustomCombinationAndNamedModeRetainsItsExactRecipeIdentityOnTheWire()
    {
        var choices = new List<ScenarioSetupChoice>();
        for (int bits = 0; bits < 16; bits++) choices.Add(Choice("custom", bits));
        choices.Add(Choice("standard", 0)); choices.Add(Choice("expert", 15)); choices.Add(Choice("extreme", 0));
        var response = new EngineResponse(EngineProtocol.Version, "setup", "", null, null, [], null, null)
            { Setup = new([], choices, [], DatasetGameFactory.Load(RepositoryPaths.Root).DiscoverSetup().Runtime) };
        var actual = EngineJson.ReadResponse(EngineJson.Write(response)).Setup!.Scenarios;
        Assert.Equal(19, actual.Count);
        Assert.Equal(19, actual.Select(choice => choice.Key).Distinct(StringComparer.Ordinal).Count());
        for (int index = 0; index < actual.Count; index++)
        {
            Assert.Equal(choices[index].Key, actual[index].Key);
            Assert.Equal(choices[index].Mode, actual[index].Mode);
            Assert.False(actual[index].AllowsModularSets);
            Assert.Equal(choices[index].Variants, actual[index].Variants);
            Assert.Equal(["Wrecker", "Thunderball", "Piledriver", "Bulldozer"], actual[index].Variants.Select(item => item.Name));
        }
    }

    private static ScenarioSetupChoice Choice(string mode, int bits)
    {
        var groups = Stages.Select((stages, index) => new EncounterGroupRecipe(
            mode == "extreme" ? stages : [stages[(bits >> index) & 1]], "unused", [])
            { Variant = mode == "extreme" ? "A → B" : ((bits >> index) & 1) == 0 ? "A" : "B" }).ToArray();
        var campaign = new CampaignSetup("Wrecking Crew", [], mode == "expert", [], [], [], [], [], [])
            { SetupMode = mode, EncounterGroups = new(groups, 0) };
        return ScenarioSetupProjection.Describe(mode + "-" + bits, campaign, Cards);
    }
}

