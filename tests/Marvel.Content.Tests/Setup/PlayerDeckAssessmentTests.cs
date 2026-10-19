using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class PlayerDeckAssessmentTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    [Theory]
    [Rule("rr:deck-customization")]
    [InlineData("spider_man")]
    [InlineData("captain_marvel")]
    [InlineData("she_hulk")]
    [InlineData("iron_man")]
    [InlineData("black_panther")]
    public void StartersAreValidWithoutChangingTheirOrder(string key)
    {
        // Appendix I: "A player's deck consists of a minimum of 40 cards
        // and a maximum of 50 cards." The glossary redirects to that appendix.
        HeroSetup hero = Setup.Hero(key);
        var recipe = new PlayerDeckRecipe([.. hero.HeroDeck, .. hero.PlayerDeck]);
        string[] before = [.. recipe.Cards];
        var result = PlayerDeckAssessment.Assess(recipe, hero.HeroDeck, hero.Hero.Single(), Cards);
        Assert.True(result.IsValid, string.Join("; ", result.Diagnostics));
        Assert.Equal(40, result.Size);
        Assert.Equal(before, recipe.Cards);
        Assert.Equal(before, PlayerDeckAdmission.Admit(recipe, hero.HeroDeck, 0, Cards, hero.Hero.Single())
            .Select(card => card.Spec));
    }

    [Fact]
    [Rule("rr:unique-icon.2")]
    public void IndependentCorrectionsAreReportedTogetherAndAdmissionRejectsTheSameDraft()
    {
        // "a player cannot include multiple matching cards in their deck."
        HeroSetup hero = Setup.Hero("spider_man");
        var draft = hero.HeroDeck.Concat(hero.PlayerDeck).ToList();
        draft.RemoveAt(0);
        draft.AddRange(["01083", "01050", "01092", "01092", "01092", "01092"]);
        var recipe = new PlayerDeckRecipe(draft);
        var result = PlayerDeckAssessment.Assess(recipe, hero.HeroDeck, hero.Hero.Single(), Cards);
        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Code == "signature-quantity");
        Assert.Contains(result.Diagnostics, item => item.Code == "multiple-aspects");
        Assert.Contains(result.Diagnostics, item => item.Code == "copy-limit");
        Assert.Contains(result.Diagnostics, item => item.Code == "unique-match" && item.Face == "01083");
        var error = Assert.Throws<ArgumentException>(() => PlayerDeckAdmission.Admit(recipe, hero.HeroDeck, 0, Cards, hero.Hero.Single()));
        foreach (var diagnostic in result.Diagnostics) Assert.Contains(diagnostic.Message, error.Message);
    }

    [Fact]
    [Rule("rr:deck-customization")]
    public void SameTitleCannotReplaceADifferentSignatureFace()
    {
        // Appendix I, Player Decks: "The exact quantity of each card included
        // in that identity set must be included in the deck."
        HeroSetup hero = Setup.Hero("black_panther");
        string[] variants = hero.HeroDeck.Distinct().GroupBy(Cards.Title)
            .First(group => group.Count() > 1).ToArray();
        var draft = hero.HeroDeck.Concat(hero.PlayerDeck).ToList();
        draft[draft.IndexOf(variants[0])] = variants[1];
        var result = PlayerDeckAssessment.Assess(new(draft), hero.HeroDeck, hero.Hero.Single(), Cards);
        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Code == "signature-quantity" && item.Face == variants[0]);
        Assert.Contains(result.Diagnostics, item => item.Code == "signature-quantity" && item.Face == variants[1]);
        Assert.Equal(40, result.Size);
    }

    [Theory]
    [InlineData(39, false)]
    [InlineData(40, true)]
    [InlineData(50, true)]
    [InlineData(51, false)]
    [Rule("rr:deck-customization")]
    public void DeckSizeIncludesBothBoundaries(int count, bool valid)
    {
        // Appendix I: "a minimum of 40 cards and a maximum of 50 cards".
        HeroSetup hero = Setup.Hero("spider_man");
        string[] additions = ["01060", "01061", "01063", "01064", "01065",
            "01085", "01085", "01086", "01086", "01087", "01087"];
        var recipe = new PlayerDeckRecipe(hero.HeroDeck.Concat(hero.PlayerDeck).Concat(additions).Take(count).ToArray());
        var result = PlayerDeckAssessment.Assess(recipe, hero.HeroDeck, hero.Hero.Single(), Cards);
        Assert.Equal(count, result.Size);
        Assert.Equal(valid, result.IsValid);
        Assert.Equal(!valid, result.Diagnostics.Any(item => item.Code == "deck-size"));
    }

    [Fact]
    public void UnknownUnsupportedAndMalformedFacesAreNotSilentlyDropped()
    {
        HeroSetup hero = Setup.Hero("spider_man");
        var authored = hero.HeroDeck.Concat(hero.PlayerDeck).ToHashSet(StringComparer.Ordinal);
        var result = CoreDeckAssessment.Assess(["unknown", "01050", "01001a,01001b", ""], hero, Cards, authored);
        Assert.False(result.IsValid);
        Assert.Null(result.Size);
        Assert.Equal(["unknown-face", "unsupported-face", "single-face-required", "single-face-required"],
            result.Diagnostics.Select(item => item.Code));
    }
}
