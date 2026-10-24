using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class WreckingAdmissionBoundaryTests
{
    [Fact]
    public void AuthoredEncounterRowsDoNotAdvertiseAnIncompleteScenarioOrPlayerDeck()
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        setup = RuntimeProductAdmission.Discover(setup, cards, AuthoredCards.Book.Authored).Setup;
        Assert.Contains("07027", AuthoredCards.Book.Authored);
        Assert.DoesNotContain("wrecking_crew", setup.CampaignNames);
        Assert.Throws<KeyNotFoundException>(() =>
            Dealer.DealOrder(setup, "wrecking_crew", ["spider_man"], facts: cards));
        foreach (string scenario in setup.CampaignNames)
            Assert.DoesNotContain(Dealer.DealOrder(setup, scenario, ["spider_man"], facts: cards),
                card => card.Spec.Split(',').Any(face => face.StartsWith("07", StringComparison.Ordinal)));

        var hero = setup.Hero("spider_man");
        var recipe = hero.HeroDeck.Concat(hero.PlayerDeck).ToArray();
        recipe[^1] = "07027";
        Assert.Throws<ArgumentException>(() => Dealer.DealOrder(setup, "rhino", ["spider_man"],
            facts: cards, playerDecks: [new PlayerDeckRecipe(recipe)]));
    }
}
