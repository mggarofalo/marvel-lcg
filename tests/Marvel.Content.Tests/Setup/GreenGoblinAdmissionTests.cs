using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class GreenGoblinAdmissionTests
{
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);
    private static readonly string[] Scenarios = ["risky_business", "risky_business_expert", "mutagen_formula", "mutagen_formula_expert"];
    private static readonly string[] Modulars = ["goblin_gimmicks", "a_mess_of_things", "power_drain", "running_interference"];

    public static IEnumerable<object[]> Faces() => Cards.Faces.Where(face => Cards.Pack(face) == "gob").Select(face => new object[] { face });
    public static IEnumerable<object[]> Recipes() => Scenarios.Concat(Modulars).Select(key => new object[] { key });
    private static readonly string[] Fields = ["villain", "schemes", "set_aside", "encounters", "encounter_sets", "modular_sets", "expert", "setup_mode", "challenges"];
    public static IEnumerable<object[]> Changes() => Scenarios.SelectMany(key =>
        Fields
            .Select(field => new object[] { key, field }));

    [Fact]
    public void CompleteReviewedInventoryAdmitsOnlyTheCompleteProduct()
    {
        var result = Discover(JsonNode.Parse(SetupJson)!);
        Assert.Equal(57, result.Playable.Count(face => Cards.Pack(face) == "gob"));
        Assert.All(Scenarios, key => Assert.Contains(key, result.Setup.CampaignNames));
        Assert.All(Modulars, key => Assert.Contains(key, result.Setup.EncounterSetNames));
        Assert.Contains("ms_marvel", result.Setup.HeroNames);
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == "ant");
    }

    [Theory]
    [MemberData(nameof(Faces))]
    public void EveryRequiredAuthoredFaceClosesAllScenariosAndModularsWhenMissing(string face)
    {
        Assert.False(StandaloneProductAdmission.GreenGoblin(SetupCatalog.Parse(SetupJson), Cards,
            AuthoredCards.Book.Authored.Where(value => value != face).ToHashSet(StringComparer.Ordinal)));
    }

    [Theory]
    [MemberData(nameof(Recipes))]
    public void EveryRequiredRecipeClosesTheWholeProductWhenMissing(string key)
    {
        var json = JsonNode.Parse(SetupJson)!;
        json[Scenarios.Contains(key) ? "campaigns" : "encounter_sets"]!.AsObject().Remove(key);
        Assert.False(StandaloneProductAdmission.GreenGoblin(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored));
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public void MeaningfullyWrongScenarioCompositionClosesTheWholeProduct(string key, string field)
    {
        var json = JsonNode.Parse(SetupJson)!;
        var recipe = json["campaigns"]![key]!;
        switch (field)
        {
            case "expert": recipe[field] = !recipe[field]!.GetValue<bool>(); break;
            case "setup_mode": recipe[field] = "extreme"; break;
            case "set_aside": recipe[field] = recipe[field]!.AsArray().Count == 0 ? new JsonArray("02006a,02006b") : new JsonArray(); break;
            case "challenges": recipe[field] = new JsonArray("01190"); break;
            default:
                var list = recipe[field]!.AsArray();
                list[0] = field switch { "modular_sets" => "bomb_scare", "encounter_sets" => "expert", "villain" => "02016", "schemes" => "02018a,02018b", _ => "02033" };
                break;
        }
        Assert.False(StandaloneProductAdmission.GreenGoblin(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored));
    }

    [Theory]
    [InlineData("goblin_gimmicks")]
    [InlineData("a_mess_of_things")]
    [InlineData("power_drain")]
    [InlineData("running_interference")]
    public void SameCountSubstitutionOrWrongRoleCannotSupplyAModularSet(string key)
    {
        var json = JsonNode.Parse(SetupJson)!;
        json["encounter_sets"]![key]!["encounters"]![0] = "02007";
        Assert.False(StandaloneProductAdmission.GreenGoblin(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored));
        json = JsonNode.Parse(SetupJson)!;
        json["encounter_sets"]![key]!["encounters"]!.AsArray().RemoveAt(0);
        Assert.False(StandaloneProductAdmission.GreenGoblin(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored));
        json = JsonNode.Parse(SetupJson)!;
        json["encounter_sets"]![key]!["role"] = "scenario";
        Assert.False(StandaloneProductAdmission.GreenGoblin(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored));
    }

    [Fact]
    public void EqualPrintedFaceCountCannotSubstituteAnUnreviewedFace()
    {
        var json = JsonNode.Parse(CardJson)!;
        json["cards"]!.AsArray().Single(card => card!["card_id"]!.GetValue<string>() == "02049")!["card_id"] = "99999";
        Assert.False(StandaloneProductAdmission.GreenGoblin(SetupCatalog.Parse(SetupJson), CardCatalog.Parse(json.ToJsonString()),
            AuthoredCards.Book.Authored.Append("99999").ToHashSet(StringComparer.Ordinal)));
    }

    [Theory]
    [InlineData("face")]
    [InlineData("scenario")]
    [InlineData("modular")]
    public void PublicAdmissionClosesBothScenariosAndAllModularSetsTogether(string component)
    {
        var json = JsonNode.Parse(SetupJson)!;
        if (component == "scenario") json["campaigns"]!.AsObject().Remove("mutagen_formula_expert");
        if (component == "modular") json["encounter_sets"]!["power_drain"]!["encounters"]!.AsArray().RemoveAt(0);
        var authored = AuthoredCards.Book.Authored.Where(face => component != "face" || face != "02049").ToHashSet(StringComparer.Ordinal);
        AssertClosed(RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), Cards, authored));
    }

    private static RuntimeProductAdmission Discover(JsonNode json) =>
        RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored);

    private static void AssertClosed(RuntimeProductAdmission result)
    {
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == "gob");
        Assert.All(Scenarios, key => Assert.DoesNotContain(key, result.Setup.CampaignNames));
        Assert.All(Modulars, key => Assert.DoesNotContain(key, result.Setup.EncounterSetNames));
        Assert.Contains("rhino", result.Setup.CampaignNames);
        Assert.Contains("crossbones", result.Setup.CampaignNames);
        Assert.NotNull(result.Campaign);
    }
}
