using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Marvel.Rules.Setup;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class DoctorStrangeAdmissionTests
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
        var hero = json["heroes"]!["doctor_strange"]!;
        switch (field)
        {
            case "missing": json["heroes"]!.AsObject().Remove("doctor_strange"); break;
            case "hero": hero[field] = new JsonArray("09001b,09001a"); break;
            // These 40 cards remain a legal deck; exact published quantities still matter.
            case "player_deck": hero[field]!.AsArray()[3] = "09018"; break;
            case "hero_deck": hero[field]!.AsArray()[0] = "09003"; break;
            case "obligations": hero[field] = new JsonArray(); break;
            case "nemesis_set": hero[field]!.AsArray().RemoveAt(4); break;
            case "nemesis_set_icon": hero[field] = "spider_man_nemesis"; break;
            case "aspect_rule": hero[field] = "equal_two_aspects"; break;
        }
        var setup = SetupCatalog.Parse(json.ToJsonString());
        if (field == "player_deck")
        {
            var wrong = setup.Hero("doctor_strange");
            Assert.Equal(40, wrong.HeroDeck.Count + wrong.PlayerDeck.Count);
            _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. wrong.HeroDeck, .. wrong.PlayerDeck]),
                wrong.HeroDeck, 0, Cards, wrong.Hero[0], wrong.AspectRule);
        }
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("doctor_strange", result.Setup.HeroNames);
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == "drs");
        Assert.Contains("captain_america", result.Setup.HeroNames);
        Assert.NotNull(result.Campaign);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingOrReorderedInvocationRecipeClosesEveryFace(bool reorder)
    {
        var json = JsonNode.Parse(SetupJson)!;
        var hero = json["heroes"]!["doctor_strange"]!.AsObject();
        if (reorder) hero["invocation_deck"] = new JsonArray("09036", "09035", "09034", "09033", "09032");
        else hero.Remove("invocation_deck");
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("doctor_strange", result.Setup.HeroNames);
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == "drs");
        Assert.Contains("spider_man", result.Setup.HeroNames);
    }

    [Fact]
    public void SameFaceCountWithAnUnreviewedFaceDoesNotAdmitThePack()
    {
        var json = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")))!;
        json["cards"]!.AsArray().Single(card => card!["card_id"]!.GetValue<string>() == "09039")!["card_id"] = "99999";
        var cards = CardCatalog.Parse(json.ToJsonString());
        var authored = AuthoredCards.Book.Authored.Append("99999").ToHashSet(StringComparer.Ordinal);
        Assert.False(StandaloneProductAdmission.DoctorStrange(SetupCatalog.Parse(SetupJson), cards, authored));
    }
}
