using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class ThorSetupTests
{
    private static readonly string[] PrintedSignature =
    [
        "06002", "06003", "06003", "06003", "06004", "06005", "06005", "06005", "06006", "06006", "06007", "06008", "06008", "06009", "06010"
    ];

    private static readonly string[] PrintedStarterExtras =
    [
        "06011", "06012", "06013", "06013", "06014", "06014", "06014", "06015", "06015", "06015", "06016", "06016", "06017", "06018", "06018", "06018", "06019", "06020", "06021", "06021", "06021", "06022", "06023", "06024", "06025"
    ];

    [Fact]
    public void StarterAndEncounterCardsMatchThePrintedDivider()
    {
        // Authority: datasets/rules-packs/mc06/UPSTREAM.md transcribes the pinned
        // FFG divider: 15 hero cards, 17 Aggression, 8 Basic, one obligation
        // and five nemesis cards. Numbered nemesis scans require two Frost Giants
        // and one Trickster. The four extra-pool cards are absent.
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var hero = setup.Hero("thor");
        Assert.Equal("Thor", hero.Name);
        Assert.Equal(["06001a,06001b"], hero.Hero);
        Assert.Equal(["06026"], hero.Obligations);
        Assert.Equal(["06027", "06028", "06029", "06029", "06030"], hero.NemesisSet);
        Assert.Equal("thor_nemesis", hero.NemesisSetIcon);
        Assert.True(MatchesPrintedStarter(hero));
        Assert.Equal(15, hero.HeroDeck.Count);
        Assert.Equal(25, hero.PlayerDeck.Count);

        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        Assert.Equal(35, cards.Faces.Count(face => cards.Pack(face) == "thor"));
        _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. hero.HeroDeck, .. hero.PlayerDeck]),
            hero.HeroDeck, 0, cards, hero.Hero[0], hero.AspectRule);
    }

    [Fact]
    public void WrongPrintedQuantityFailsEvenWhenTheDeckRemainsLegal()
    {
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var hero = setup.Hero("thor");
        var wrongExtras = hero.PlayerDeck.ToArray();
        wrongExtras[Array.IndexOf(wrongExtras, "06014")] = "06013";
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

        Assert.Contains("thor", setup.HeroNames);
        Assert.Contains("thor", admitted.Setup.HeroNames);
        Assert.Equal(35, admitted.Playable.Count(face => cards.Pack(face) == "thor"));
        Assert.Equal(11, admitted.Setup.HeroNames.Count);
        Assert.Contains("spider_man", admitted.Setup.HeroNames);
    }

    private static bool MatchesPrintedStarter(HeroSetup hero) =>
        hero.HeroDeck.SequenceEqual(PrintedSignature)
        && hero.PlayerDeck.SequenceEqual(PrintedStarterExtras);
}
