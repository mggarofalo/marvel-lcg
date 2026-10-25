using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class CaptainAmericaSetupTests
{
    [Fact]
    public void StarterAndEncounterCardsMatchThePrintedDivider()
    {
        // Authority: datasets/rules-packs/mc04/UPSTREAM.md transcribes the pinned
        // FFG divider: 15 hero cards, 17 Leadership, eight Basic, one obligation
        // and five nemesis cards.
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var hero = setup.Hero("captain_america");
        Assert.Equal(["03001a,03001b"], hero.Hero);
        Assert.Equal(["03026"], hero.Obligations);
        Assert.Equal(["03027", "03028", "03029", "03029", "03030"], hero.NemesisSet);
        Assert.Equal("captain_america_nemesis", hero.NemesisSetIcon);
        Assert.Equal([1, 2, 3, 2, 2, 1, 1, 1, 2],
            Enumerable.Range(2, 9).Select(number => hero.HeroDeck.Count(face => face == $"03{number:000}")));
        Assert.Equal([1, 1, 1, 1, 3, 2, 3, 2, 3, 1, 1, 1, 1, 1, 3],
            Enumerable.Range(11, 15).Select(number => hero.PlayerDeck.Count(face => face == $"03{number:000}")));
        Assert.Equal(15, hero.HeroDeck.Count);
        Assert.Equal(25, hero.PlayerDeck.Count);
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. hero.HeroDeck, .. hero.PlayerDeck]),
            hero.HeroDeck, 0, cards, hero.Hero[0], hero.AspectRule);
    }

    [Fact]
    public void CompleteProductSuppliedAsAuthoredEntersRuntimeAdmission()
    {
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var admitted = RuntimeProductAdmission.Discover(setup, cards, cards.Faces.ToHashSet(StringComparer.Ordinal));
        Assert.Contains("captain_america", admitted.Setup.HeroNames);
        Assert.Contains("03001a", admitted.Playable);
        Assert.Contains("spider_man", admitted.Setup.HeroNames);
        Assert.Contains("hawkeye", admitted.Setup.HeroNames);
    }
}
