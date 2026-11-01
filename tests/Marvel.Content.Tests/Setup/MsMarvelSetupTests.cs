using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class MsMarvelSetupTests
{
    private static readonly string[] PrintedSignature =
    [
        "05002", "05003", "05003", "05003", "05004", "05004", "05004",
        "05005", "05005", "05006", "05007", "05008", "05009", "05010", "05011",
    ];

    private static readonly string[] PrintedStarterExtras =
    [
        "05012", "05013", "05013", "05014", "05014", "05014",
        "05015", "05015", "05015", "05016", "05016",
        "05017", "05017", "05017", "05018", "05019", "05020", "05021",
        "05022", "05023", "05023", "05023", "05024", "05024", "05024",
    ];

    [Fact]
    public void StarterAndEncounterCardsMatchThePrintedDivider()
    {
        // Authority: datasets/rules-packs/mc05/UPSTREAM.md transcribes the pinned
        // FFG divider: 15 hero cards, 14 Protection, 11 Basic, one obligation
        // and five nemesis cards. The four extra-pool cards are absent.
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var hero = setup.Hero("ms_marvel");
        Assert.Equal("Ms. Marvel", hero.Name);
        Assert.Equal(["05001a,05001b"], hero.Hero);
        Assert.Equal(["05025"], hero.Obligations);
        Assert.Equal(["05026", "05027", "05028", "05029", "05029"], hero.NemesisSet);
        Assert.Equal("ms_marvel_nemesis", hero.NemesisSetIcon);
        Assert.True(MatchesPrintedStarter(hero));
        Assert.Equal(15, hero.HeroDeck.Count);
        Assert.Equal(25, hero.PlayerDeck.Count);

        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        Assert.Equal(34, cards.Faces.Count(face => cards.Pack(face) == "msm"));
        _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. hero.HeroDeck, .. hero.PlayerDeck]),
            hero.HeroDeck, 0, cards, hero.Hero[0], hero.AspectRule);
    }

    [Fact]
    public void WrongPrintedQuantityFailsEvenWhenTheDeckRemainsLegal()
    {
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var hero = setup.Hero("ms_marvel");
        var wrongExtras = hero.PlayerDeck.ToArray();
        wrongExtras[Array.IndexOf(wrongExtras, "05014")] = "05013";
        var wrong = hero with { PlayerDeck = wrongExtras };

        Assert.False(MatchesPrintedStarter(wrong));
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. wrong.HeroDeck, .. wrong.PlayerDeck]),
            wrong.HeroDeck, 0, cards, wrong.Hero[0], wrong.AspectRule);
    }

    [Fact]
    public void CompleteRecipeEntersRuntimeDiscovery()
    {
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var admitted = RuntimeProductAdmission.Discover(setup, cards,
            cards.Faces.ToHashSet(StringComparer.Ordinal));

        Assert.Contains("ms_marvel", setup.HeroNames);
        Assert.Contains("ms_marvel", admitted.Setup.HeroNames);
        Assert.Equal(34, admitted.Playable.Count(face => cards.Pack(face) == "msm"));
        Assert.Equal(18, admitted.Setup.HeroNames.Count);
        Assert.Contains("spider_man", admitted.Setup.HeroNames);
    }

    private static bool MatchesPrintedStarter(HeroSetup hero) =>
        hero.HeroDeck.SequenceEqual(PrintedSignature)
        && hero.PlayerDeck.SequenceEqual(PrintedStarterExtras);
}
