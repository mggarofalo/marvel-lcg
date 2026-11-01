using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class RocketRaccoonAdmissionTests
{
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);

    // Reviewed against the generated catalog; both identity faces count.
    private static readonly string[] Faces =
        ["16029a", "16029b", .. Enumerable.Range(30, 28).Select(number => $"16{number:000}")];

    public static TheoryData<string> PrintedFaces => new(Faces);

    [Fact]
    public void TheCompleteInventoryAndStarterAdmitRocketRaccoon()
    {
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, AuthoredCards.Book.Authored);
        Assert.Contains("rocket_raccoon", result.Setup.HeroNames);
        Assert.All(Faces, face => Assert.Contains(face, result.Playable));
        var hero = result.Setup.Hero("rocket_raccoon");
        Assert.Equal(15, hero.HeroDeck.Count);
        Assert.Equal(25, hero.PlayerDeck.Count);
        Assert.Equal(["16053"], hero.Obligations);
        Assert.Equal(["16054", "16055", "16056", "16057", "16057"], hero.NemesisSet);
    }

    [Fact]
    public void TheStarterIsSeventeenAggressionAndEightBasic()
    {
        // MarvelSDB quantities: Bug, Chase Them Down ×2, Into the Fray ×3, Looking for
        // Trouble ×3, Relentless Assault ×2, Follow Through ×3 and Hand Cannon ×3 are
        // Aggression; Groot, Flora and Fauna, Energy, Genius, Strength and Booster
        // Boots ×3 are Basic.
        var hero = SetupCatalog.Parse(SetupJson).Hero("rocket_raccoon");
        string[] aggression = ["16040", "16041", "16042", "16043", "16044", "16045", "16046"];
        Assert.Equal(17, hero.PlayerDeck.Count(aggression.Contains));
        Assert.Equal(8, hero.PlayerDeck.Count(face => !aggression.Contains(face)));
    }

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
            var wrong = setup.Hero("rocket_raccoon");
            Assert.Equal(40, wrong.HeroDeck.Count + wrong.PlayerDeck.Count);
            _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. wrong.HeroDeck, .. wrong.PlayerDeck]),
                wrong.HeroDeck, 0, Cards, wrong.Hero[0], wrong.AspectRule);
        }
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("rocket_raccoon", result.Setup.HeroNames);
        Assert.DoesNotContain(result.Playable, Faces.Contains);
        Assert.Contains("groot", result.Setup.HeroNames);
        Assert.Contains("ant_man", result.Setup.HeroNames);
        Assert.NotNull(result.Campaign);
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void MissingAuthoredFaceClosesRuntimePackAndPreservesOtherProducts(string face)
    {
        var authored = AuthoredCards.Book.Authored.Where(id => id != face).ToHashSet(StringComparer.Ordinal);
        var admitted = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, authored);
        Assert.DoesNotContain("rocket_raccoon", admitted.Setup.HeroNames);
        Assert.DoesNotContain(admitted.Playable, Faces.Contains);
        Assert.Equal(17, admitted.Setup.HeroNames.Count);
        Assert.Contains("groot", admitted.Setup.HeroNames);
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void ChangedProductMembershipCannotSatisfyInventoryEvenWhenAuthored(string face)
    {
        var json = JsonNode.Parse(CardJson)!;
        json["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == face)!["pack"] = "unreviewed";
        var cards = CardCatalog.Parse(json.ToJsonString());
        Assert.False(StandaloneProductAdmission.RocketRaccoon(SetupCatalog.Parse(SetupJson), cards, AuthoredCards.Book.Authored));
    }

    private static SetupCatalog ChangedRecipe(string field)
    {
        var json = JsonNode.Parse(SetupJson)!;
        var hero = json["heroes"]!["rocket_raccoon"]!;
        switch (field)
        {
            case "missing": json["heroes"]!.AsObject().Remove("rocket_raccoon"); break;
            case "hero": hero[field] = new JsonArray("16029a"); break;
            // These 40 cards remain a legal deck; exact published quantities still matter.
            case "player_deck": hero[field]!.AsArray()[1] = "16044"; break;
            case "hero_deck": hero[field]!.AsArray()[0] = "16031"; break;
            case "obligations": hero[field] = new JsonArray(); break;
            case "nemesis_set": hero[field]!.AsArray().RemoveAt(4); break;
            case "nemesis_set_icon": hero[field] = "groot_nemesis"; break;
            case "invocation_deck": hero[field] = new JsonArray("09032", "09033", "09034", "09035", "09036"); break;
            case "aspect_rule": hero[field] = "equal_two_aspects"; break;
        }
        return SetupCatalog.Parse(json.ToJsonString());
    }
}
