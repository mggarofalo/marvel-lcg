using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class GrootAdmissionTests
{
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);

    // Reviewed against the generated catalog; both identity faces count.
    private static readonly string[] Faces =
        ["16001a", "16001b", .. Enumerable.Range(2, 27).Select(number => $"16{number:000}")];

    public static TheoryData<string> PrintedFaces => new(Faces);

    [Fact]
    public void TheCompleteInventoryAndStarterAdmitGroot()
    {
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, AuthoredCards.Book.Authored);
        Assert.Contains("groot", result.Setup.HeroNames);
        Assert.Equal(29, result.Playable.Count(face => Cards.Pack(face) == "gmw"));
        var hero = result.Setup.Hero("groot");
        Assert.Equal(40, hero.HeroDeck.Count + hero.PlayerDeck.Count);
    }

    [Fact]
    public void GrootOpensOnlyHisOwnFacesOfTheBox()
    {
        // Rocket Raccoon's identity is outside the boundary even when a row exists for it.
        var authored = AuthoredCards.Book.Authored.Append("16029a").ToHashSet(StringComparer.Ordinal);
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, authored);
        Assert.Equal(Faces.Order(StringComparer.Ordinal),
            result.Playable.Where(face => Cards.Pack(face) == "gmw").Order(StringComparer.Ordinal));
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
            var wrong = setup.Hero("groot");
            Assert.Equal(40, wrong.HeroDeck.Count + wrong.PlayerDeck.Count);
            _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. wrong.HeroDeck, .. wrong.PlayerDeck]),
                wrong.HeroDeck, 0, Cards, wrong.Hero[0], wrong.AspectRule);
        }
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("groot", result.Setup.HeroNames);
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == "gmw");
        Assert.Contains("ant_man", result.Setup.HeroNames);
        Assert.NotNull(result.Campaign);
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void MissingAuthoredFaceClosesRuntimePackAndPreservesOtherProducts(string face)
    {
        var authored = AuthoredCards.Book.Authored.Where(id => id != face).ToHashSet(StringComparer.Ordinal);
        var admitted = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, authored);
        Assert.DoesNotContain("groot", admitted.Setup.HeroNames);
        Assert.DoesNotContain(admitted.Playable, id => Cards.Pack(id) == "gmw");
        Assert.Equal(16, admitted.Setup.HeroNames.Count);
        Assert.Contains("ant_man", admitted.Setup.HeroNames);
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void ChangedProductMembershipCannotSatisfyInventoryEvenWhenAuthored(string face)
    {
        var json = JsonNode.Parse(CardJson)!;
        json["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == face)!["pack"] = "unreviewed";
        var cards = CardCatalog.Parse(json.ToJsonString());
        Assert.False(StandaloneProductAdmission.Groot(SetupCatalog.Parse(SetupJson), cards, AuthoredCards.Book.Authored));
    }

    private static SetupCatalog ChangedRecipe(string field)
    {
        var json = JsonNode.Parse(SetupJson)!;
        var hero = json["heroes"]!["groot"]!;
        switch (field)
        {
            case "missing": json["heroes"]!.AsObject().Remove("groot"); break;
            case "hero": hero[field] = new JsonArray("16001a"); break;
            // These 40 cards remain a legal deck; exact published quantities still matter.
            case "player_deck": hero[field]!.AsArray()[1] = "16018"; break;
            case "hero_deck": hero[field]!.AsArray()[0] = "16003"; break;
            case "obligations": hero[field] = new JsonArray(); break;
            case "nemesis_set": hero[field]!.AsArray().RemoveAt(4); break;
            case "nemesis_set_icon": hero[field] = "ant_nemesis"; break;
            case "invocation_deck": hero[field] = new JsonArray("09032", "09033", "09034", "09035", "09036"); break;
            case "aspect_rule": hero[field] = "equal_two_aspects"; break;
        }
        return SetupCatalog.Parse(json.ToJsonString());
    }
}
