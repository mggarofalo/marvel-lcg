using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class BlackWidowSetupTests
{
    [Fact]
    public void PrintedDividerSuppliesFortyCardsAndSeparateEncounterComponents()
    {
        // Authority: datasets/rules-packs/mc07/UPSTREAM.md pins the FFG divider:
        // 15 hero, 17 Justice and 8 Basic; obligation and nemesis are separate.
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var hero = setup.Hero("black_widow");
        Assert.Equal("Black Widow", hero.Name);
        Assert.Equal(["08001a,08001b"], hero.Hero);
        Assert.Equal(["08025"], hero.Obligations);
        Assert.Equal(["08026", "08027", "08028", "08028", "08029"], hero.NemesisSet);
        Assert.Equal("black_widow_nemesis", hero.NemesisSetIcon);
        Assert.Equal(
            ["08002", "08003", "08003", "08004", "08004", "08005", "08006", "08006",
                "08007", "08007", "08008", "08008", "08009", "08010", "08010"], hero.HeroDeck);
        Assert.Equal(
            ["08011", "08012", "08013", "08013", "08013", "08014", "08014", "08015",
                "08015", "08016", "08016", "08017", "08017", "08017", "08018", "08018",
                "08018", "08019", "08020", "08021", "08022", "08023", "08024", "08024", "08024"],
            hero.PlayerDeck);
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        Assert.Equal(34, cards.Faces.Count(face => cards.Pack(face) == "bkw"));
        _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. hero.HeroDeck, .. hero.PlayerDeck]),
            hero.HeroDeck, 0, cards, hero.Hero[0], hero.AspectRule);
    }

    [Fact]
    public void CompleteRecipeEntersRuntimeDiscovery()
    {
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var admitted = RuntimeProductAdmission.Discover(setup, cards, cards.Faces.ToHashSet(StringComparer.Ordinal));
        Assert.Contains("black_widow", setup.HeroNames);
        Assert.Contains("black_widow", admitted.Setup.HeroNames);
        Assert.Equal(34, admitted.Playable.Count(face => cards.Pack(face) == "bkw"));
        Assert.Contains("thor", admitted.Setup.HeroNames);
        Assert.Equal(17, admitted.Setup.HeroNames.Count);
    }
}
