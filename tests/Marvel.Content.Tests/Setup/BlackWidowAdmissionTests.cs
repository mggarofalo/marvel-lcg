using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Marvel.Rules.Setup;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class BlackWidowAdmissionTests
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
        var hero = json["heroes"]!["black_widow"]!;
        switch (field)
        {
            case "missing": json["heroes"]!.AsObject().Remove("black_widow"); break;
            case "hero": hero[field] = new JsonArray("08001b,08001a"); break;
            // These 40 cards remain a legal deck; exact published quantities still matter.
            case "player_deck": hero[field]!.AsArray()[2] = "08015"; break;
            case "hero_deck": hero[field]!.AsArray()[0] = "08003"; break;
            case "obligations": hero[field] = new JsonArray(); break;
            case "nemesis_set": hero[field]!.AsArray().RemoveAt(4); break;
            case "nemesis_set_icon": hero[field] = "spider_man_nemesis"; break;
            case "aspect_rule": hero[field] = "equal_two_aspects"; break;
        }
        var setup = SetupCatalog.Parse(json.ToJsonString());
        if (field == "player_deck")
        {
            var wrong = setup.Hero("black_widow");
            Assert.Equal(40, wrong.HeroDeck.Count + wrong.PlayerDeck.Count);
            _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. wrong.HeroDeck, .. wrong.PlayerDeck]),
                wrong.HeroDeck, 0, Cards, wrong.Hero[0], wrong.AspectRule);
        }
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("black_widow", result.Setup.HeroNames);
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == "bkw");
        Assert.Contains("captain_america", result.Setup.HeroNames);
        Assert.NotNull(result.Campaign);
    }

    [Fact]
    public void SameFaceCountWithAnUnreviewedFaceDoesNotAdmitThePack()
    {
        var json = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")))!;
        json["cards"]!.AsArray().Single(card => card!["card_id"]!.GetValue<string>() == "08033")!["card_id"] = "99999";
        var cards = CardCatalog.Parse(json.ToJsonString());
        var authored = AuthoredCards.Book.Authored.Append("99999").ToHashSet(StringComparer.Ordinal);
        Assert.False(StandaloneProductAdmission.BlackWidow(SetupCatalog.Parse(SetupJson), cards, authored));
    }
}
