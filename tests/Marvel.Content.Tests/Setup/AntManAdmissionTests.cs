using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Marvel.Rules.Setup;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class AntManAdmissionTests
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
    [InlineData("invocation_deck")]
    public void MissingOrWrongPublishedRecipeClosesEveryFace(string field)
    {
        var setup = ChangedRecipe(field);
        if (field == "player_deck")
        {
            var wrong = setup.Hero("ant_man");
            Assert.Equal(40, wrong.HeroDeck.Count + wrong.PlayerDeck.Count);
            _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. wrong.HeroDeck, .. wrong.PlayerDeck]),
                wrong.HeroDeck, 0, Cards, wrong.Hero[0], wrong.AspectRule);
        }
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("ant_man", result.Setup.HeroNames);
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == "ant");
        Assert.Contains("captain_america", result.Setup.HeroNames);
        Assert.NotNull(result.Campaign);
    }

    [Fact]
    public void SameFaceCountWithAnUnreviewedFaceDoesNotAdmitThePack()
    {
        var json = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")))!;
        json["cards"]!.AsArray().Single(card => card!["card_id"]!.GetValue<string>() == "12033")!["card_id"] = "99999";
        var cards = CardCatalog.Parse(json.ToJsonString());
        var authored = AuthoredCards.Book.Authored.Append("99999").ToHashSet(StringComparer.Ordinal);
        Assert.False(StandaloneProductAdmission.AntMan(SetupCatalog.Parse(SetupJson), cards, authored));
    }

    private static SetupCatalog ChangedRecipe(string field)
    {
        var json = JsonNode.Parse(SetupJson)!;
        var hero = json["heroes"]!["ant_man"]!;
        switch (field)
        {
            case "missing": json["heroes"]!.AsObject().Remove("ant_man"); break;
            case "hero": hero[field] = new JsonArray("12001a,12001b"); break;
            // These 40 cards remain a legal deck; exact published quantities still matter.
            case "player_deck": hero[field]!.AsArray()[24] = "12033"; break;
            case "hero_deck": hero[field]!.AsArray()[0] = "12003"; break;
            case "obligations": hero[field] = new JsonArray(); break;
            case "nemesis_set": hero[field]!.AsArray().RemoveAt(4); break;
            case "nemesis_set_icon": hero[field] = "spider_man_nemesis"; break;
            case "invocation_deck": hero[field] = new JsonArray("09032", "09033", "09034", "09035", "09036"); break;
            case "aspect_rule": hero[field] = "equal_two_aspects"; break;
        }
        return SetupCatalog.Parse(json.ToJsonString());
    }

}
