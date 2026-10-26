using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class MsMarvelAdmissionTests
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
        var hero = json["heroes"]!["ms_marvel"]!;
        switch (field)
        {
            case "missing": json["heroes"]!.AsObject().Remove("ms_marvel"); break;
            case "hero": hero[field] = new JsonArray("05001b,05001a"); break;
            // These 40 cards remain a legal deck; exact published quantities still matter.
            case "player_deck": hero[field]!.AsArray()[3] = "05013"; break;
            case "hero_deck": hero[field]!.AsArray()[0] = "05003"; break;
            case "obligations": hero[field] = new JsonArray(); break;
            case "nemesis_set": hero[field]!.AsArray().RemoveAt(4); break;
            case "nemesis_set_icon": hero[field] = "spider_man_nemesis"; break;
            case "aspect_rule": hero[field] = "equal_two_aspects"; break;
        }
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("ms_marvel", result.Setup.HeroNames);
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == "msm");
        Assert.Contains("captain_america", result.Setup.HeroNames);
        Assert.NotNull(result.Campaign);
    }

    [Fact]
    public void SameFaceCountWithAnUnreviewedFaceDoesNotAdmitThePack()
    {
        var json = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")))!;
        json["cards"]!.AsArray().Single(card => card!["card_id"]!.GetValue<string>() == "05033")!["card_id"] = "99999";
        var cards = CardCatalog.Parse(json.ToJsonString());
        var authored = AuthoredCards.Book.Authored.Append("99999").ToHashSet(StringComparer.Ordinal);
        Assert.False(StandaloneProductAdmission.MsMarvel(SetupCatalog.Parse(SetupJson), cards, authored));
    }
}
