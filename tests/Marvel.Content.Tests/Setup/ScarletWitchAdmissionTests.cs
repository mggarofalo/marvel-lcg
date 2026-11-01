using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class ScarletWitchAdmissionTests
{
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);

    // Reviewed against the generated catalog; both identity faces count.
    public static TheoryData<string> PrintedFaces => new(
        ["15001a", "15001b", .. Enumerable.Range(2, 30).Select(number => $"15{number:000}")]);

    [Fact]
    public void TheCompleteInventoryAndStarterAdmitScarletWitch()
    {
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, AuthoredCards.Book.Authored);
        Assert.Contains("scarlet_witch", result.Setup.HeroNames);
        Assert.Equal(32, result.Playable.Count(face => Cards.Pack(face) == "scw"));
        var hero = result.Setup.Hero("scarlet_witch");
        Assert.Equal(40, hero.HeroDeck.Count + hero.PlayerDeck.Count);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("hero")]
    [InlineData("hero_deck")]
    [InlineData("player_deck")]
    [InlineData("obligations")]
    [InlineData("obligation_copy")]
    [InlineData("nemesis_set")]
    [InlineData("nemesis_set_icon")]
    [InlineData("aspect_rule")]
    [InlineData("invocation_deck")]
    public void MissingOrWrongPublishedRecipeClosesEveryFace(string field)
    {
        var setup = ChangedRecipe(field);
        if (field == "player_deck")
        {
            var wrong = setup.Hero("scarlet_witch");
            Assert.Equal(40, wrong.HeroDeck.Count + wrong.PlayerDeck.Count);
            _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. wrong.HeroDeck, .. wrong.PlayerDeck]),
                wrong.HeroDeck, 0, Cards, wrong.Hero[0], wrong.AspectRule);
        }
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("scarlet_witch", result.Setup.HeroNames);
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == "scw");
        Assert.Contains("ant_man", result.Setup.HeroNames);
        Assert.NotNull(result.Campaign);
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void MissingAuthoredFaceClosesRuntimePackAndPreservesOtherProducts(string face)
    {
        var authored = AuthoredCards.Book.Authored.Where(id => id != face).ToHashSet(StringComparer.Ordinal);
        var admitted = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, authored);
        Assert.DoesNotContain("scarlet_witch", admitted.Setup.HeroNames);
        Assert.DoesNotContain(admitted.Playable, id => Cards.Pack(id) == "scw");
        Assert.Equal(18, admitted.Setup.HeroNames.Count);
        Assert.Contains("ant_man", admitted.Setup.HeroNames);
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void ChangedProductMembershipCannotSatisfyInventoryEvenWhenAuthored(string face)
    {
        var json = JsonNode.Parse(CardJson)!;
        json["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == face)!["pack"] = "unreviewed";
        var cards = CardCatalog.Parse(json.ToJsonString());
        Assert.False(StandaloneProductAdmission.ScarletWitch(SetupCatalog.Parse(SetupJson), cards, AuthoredCards.Book.Authored));
    }

    // These 40 cards remain a legal deck; exact published quantities still matter.
    private static void ChangeList(JsonArray list, string field)
    {
        if (field == "player_deck") list[24] = "15031";
        else if (field == "hero_deck") list[0] = "15004";
        else list.RemoveAt(3);
    }

    private static SetupCatalog ChangedRecipe(string field)
    {
        var json = JsonNode.Parse(SetupJson)!;
        var hero = json["heroes"]!["scarlet_witch"]!;
        if (field == "missing") json["heroes"]!.AsObject().Remove("scarlet_witch");
        else if (field == "obligation_copy") hero["obligations"]!.AsArray().RemoveAt(1);
        else if (field is "player_deck" or "hero_deck" or "nemesis_set") ChangeList(hero[field]!.AsArray(), field);
        else hero[field] = field switch
        {
            "hero" => (JsonNode)new JsonArray("15001a"),
            "obligations" => new JsonArray(),
            "nemesis_set_icon" => JsonValue.Create("ant_nemesis"),
            "invocation_deck" => new JsonArray("09032", "09033", "09034", "09035", "09036"),
            _ => JsonValue.Create("equal_two_aspects"),
        };
        return SetupCatalog.Parse(json.ToJsonString());
    }
}
