using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class ThorAdmissionTests
{
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("missing")]
    [InlineData("hero")]
    [InlineData("hero_deck")]
    [InlineData("player_deck")]
    [InlineData("obligations")]
    [InlineData("nemesis_set")]
    [InlineData("nemesis_set_icon")]
    [InlineData("aspect_rule")]
    public void MissingOrWrongPublishedRecipeClosesEveryFace(string field)
    {
        var json = JsonNode.Parse(SetupJson)!;
        var hero = json["heroes"]!["thor"]!;
        switch (field)
        {
            case "missing": json["heroes"]!.AsObject().Remove("thor"); break;
            case "hero": hero[field] = new JsonArray("06001b,06001a"); break;
            // These 40 cards remain a legal deck; exact published quantities still matter.
            case "player_deck": hero[field]!.AsArray()[4] = "06013"; break;
            case "hero_deck": hero[field]!.AsArray()[0] = "06003"; break;
            case "obligations": hero[field] = new JsonArray(); break;
            case "nemesis_set": hero[field]!.AsArray().RemoveAt(4); break;
            case "nemesis_set_icon": hero[field] = "spider_man_nemesis"; break;
            case "aspect_rule": hero[field] = "equal_two_aspects"; break;
        }
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("thor", result.Setup.HeroNames);
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == "thor");
        Assert.Contains("captain_america", result.Setup.HeroNames);
        Assert.NotNull(result.Campaign);
    }

    [Fact]
    public void SameFaceCountWithAnUnreviewedFaceDoesNotAdmitThePack()
    {
        var json = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")))!;
        json["cards"]!.AsArray().Single(card => card!["card_id"]!.GetValue<string>() == "06034")!["card_id"] = "99999";
        var cards = CardCatalog.Parse(json.ToJsonString());
        var authored = AuthoredCards.Book.Authored.Append("99999").ToHashSet(StringComparer.Ordinal);
        Assert.False(StandaloneProductAdmission.Thor(SetupCatalog.Parse(SetupJson), cards, authored));
    }
}
