using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class StandaloneProductCompositionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));

    [Theory]
    [InlineData("hero_deck")]
    [InlineData("obligations")]
    [InlineData("nemesis_set")]
    [InlineData("player_deck")]
    public void MissingPrintedCaptainComponentClosesWholeProduct(string field)
    {
        var json = JsonNode.Parse(SetupJson)!;
        json["heroes"]!["captain_america"]![field]!.AsArray().RemoveAt(0);
        AssertClosed(json, "cap");
    }

    [Theory]
    [InlineData("nemesis_set_icon", "wrong_nemesis")]
    [InlineData("aspect_rule", "equal_two_aspects")]
    public void ChangedCaptainAssociationClosesWholeProduct(string field, string value)
    {
        var json = JsonNode.Parse(SetupJson)!;
        json["heroes"]!["captain_america"]![field] = value;
        AssertClosed(json, "cap");
    }

    [Fact]
    public void LegalCustomDeckRemainsAvailableWithoutReplacingPublishedStarter()
    {
        var setup = SetupCatalog.Parse(SetupJson);
        var hero = setup.Hero("captain_america");
        string[] alternate = [.. hero.HeroDeck, .. hero.PlayerDeck.Skip(1), "03016"];
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.Contains("captain_america", result.Setup.HeroNames);
        var admitted = PlayerDeckAdmission.Admit(new PlayerDeckRecipe(alternate), hero.HeroDeck,
            0, Cards, hero.Hero[0], hero.AspectRule);
        Assert.Equal(40, admitted.Count);
        Assert.Equal(3, admitted.Count(card => card.Spec == "03016"));
    }

    private static readonly string[] Changes = ["mode", "expert", "active", "stage", "scheme", "encounters", "variant", "main", "extra"];

    public static IEnumerable<object[]> WreckingChanges() => SetupCatalog.Parse(SetupJson).CampaignNames
        .Where(key => key.StartsWith("wrecking_crew_", StringComparison.Ordinal))
        .SelectMany(key => Changes
            .Select(change => new object[] { key, change }));

    [Theory]
    [MemberData(nameof(WreckingChanges))]
    public void ChangedWreckingCompositionClosesEveryMode(string key, string change)
    {
        var json = JsonNode.Parse(SetupJson)!;
        var recipe = json["campaigns"]![key]!;
        var groups = recipe["encounter_groups"]!;
        var first = groups["groups"]![0]!;
        switch (change)
        {
            case "mode": recipe.AsObject().Remove("setup_mode"); break;
            case "expert": recipe["expert"] = !recipe["expert"]!.GetValue<bool>(); break;
            case "active": groups["active_group"] = 1; break;
            case "stage": first["stages"]![0] = "07017"; break;
            case "scheme": first["scheme"] = "07019"; break;
            case "encounters": first["encounters"]![0] = "07006"; break;
            case "variant": first["variant"] = "wrong"; break;
            case "main": recipe["schemes"]![0] = "01097a,01097b"; break;
            case "extra": recipe["set_aside"]!.AsArray().Add("07005"); break;
        }
        Assert.False(StandaloneProductAdmission.WreckingCrew(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored));
    }

    private static void AssertClosed(JsonNode json, string pack)
    {
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == pack);
        Assert.DoesNotContain("captain_america", pack == "cap" ? result.Setup.HeroNames : []);
        Assert.DoesNotContain(result.Setup.CampaignNames, key => pack == "twc" && key.StartsWith("wrecking_crew_", StringComparison.Ordinal));
        Assert.Contains("hawkeye", result.Setup.HeroNames);
        Assert.NotNull(result.Campaign);
        Assert.Contains(pack == "cap" ? "07002" : "03001a", result.Playable);
    }
}
