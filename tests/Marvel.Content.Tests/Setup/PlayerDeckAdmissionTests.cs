using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class PlayerDeckAdmissionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    [Fact]
    public void ExplicitDeckPreservesItsOrderingAndCapturesCallerInput()
    {
        HeroSetup hero = Setup.Hero("spider_man");
        var supplied = hero.PlayerDeck.Concat(hero.HeroDeck).ToList();
        var recipe = new PlayerDeckRecipe(supplied);
        string first = supplied[0];
        supplied[0] = "invalid";

        var dealt = Dealer.DealOrder(Setup, "rhino", ["spider_man"], facts: Cards, playerDecks: [recipe]);

        Assert.Equal(first, recipe.Cards[0]);
        Assert.Equal(recipe.Cards, dealt.Where(card => card.Source is CreationSource.HeroDeck or CreationSource.PlayerDeck)
            .Select(card => card.Spec));
    }

    [Fact]
    public void FiftyCardDeckIsLegalAndFiftyOneIsNot()
    {
        HeroSetup hero = Setup.Hero("spider_man");
        string[] additions = ["01060", "01061", "01063", "01064", "01065",
            "01085", "01085", "01086", "01086", "01087"];
        var recipe = new PlayerDeckRecipe([.. hero.HeroDeck, .. hero.PlayerDeck, .. additions]);
        var dealt = Dealer.DealOrder(Setup, "rhino", ["spider_man"], facts: Cards, playerDecks: [recipe]);
        Assert.Equal(50, DeckConstruction.DeckSize(dealt, Cards));
        var oversized = new PlayerDeckRecipe([.. recipe.Cards, "01087"]);
        Assert.Throws<ArgumentException>(() => Dealer.DealOrder(Setup, "rhino", ["spider_man"],
            facts: Cards, playerDecks: [oversized]));
    }

    [Theory]
    [InlineData("spider_man")]
    [InlineData("captain_marvel")]
    [InlineData("she_hulk")]
    [InlineData("iron_man")]
    [InlineData("black_panther")]
    public void EveryAuthoredStarterIsAlsoAValidExplicitRecipe(string identity)
    {
        HeroSetup hero = Setup.Hero(identity);
        var dealt = Dealer.DealOrder(Setup, "rhino", [identity], facts: Cards,
            playerDecks: [new PlayerDeckRecipe([.. hero.HeroDeck, .. hero.PlayerDeck])]);
        Assert.Equal(40, DeckConstruction.DeckSize(dealt, Cards));
    }

    [Theory]
    [InlineData("missing-signature")]
    [InlineData("too-small")]
    [InlineData("printed-limit")]
    [InlineData("extra-signature")]
    [InlineData("two-aspects")]
    [InlineData("four-copies")]
    [InlineData("unique-copy")]
    [InlineData("forged-faces")]
    public void ExplicitDeckRejectsIllegalComposition(string defect)
    {
        // Core Learn to Play customization rules: exact hero-set quantities,
        // 40–50 cards, one aspect plus basic, and printed/unique copy limits.
        HeroSetup hero = Setup.Hero("spider_man");
        var cards = hero.HeroDeck.Concat(hero.PlayerDeck).ToList();
        switch (defect)
        {
            case "missing-signature": cards.RemoveAt(0); cards.Add("01092"); break;
            case "too-small": cards.RemoveAt(cards.Count - 1); break;
            case "printed-limit": cards.Add("01088"); break;
            case "extra-signature": cards.Add(hero.HeroDeck[0]); break;
            case "two-aspects": cards.Add("01050"); break;
            case "four-copies": cards.AddRange(Enumerable.Repeat("01092", 4)); break;
            case "unique-copy": cards.Add("01083"); break;
            case "forged-faces": cards[^1] += ",01001a"; break;
        }

        Assert.Throws<ArgumentException>(() => Dealer.DealOrder(Setup, "rhino", ["spider_man"],
            facts: Cards, playerDecks: [new PlayerDeckRecipe(cards)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void UnsupportedTableSizesAreRejected(int players) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PlayerCountRules.Validate(players));
}
