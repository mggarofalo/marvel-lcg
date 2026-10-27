using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests;

public sealed class NemesisMinionCatalogTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    [Rule("rr:nemesis-encounter-set.3.1")]
    public void PrintedDesignationDistinguishesMinionsWithTheSameNemesisAttribute()
    {
        // With multiple minions, the nemesis minion "is designated by
        // parenthetical text printed on one or more of those minions".
        Assert.Equal(Cards.Attributes("08026")["Nemesis"], Cards.Attributes("08028")["Nemesis"]);
        Assert.True(Cards.IsNemesisMinion("08026"));
        Assert.False(Cards.IsNemesisMinion("08028"));
        Assert.True(Cards.IsNemesisMinion("05027"));
        Assert.False(Cards.IsNemesisMinion("05028"));
        Assert.False(Cards.IsNemesisMinion("08027"));
        Assert.False(Cards.IsNemesisMinion("01101"));
    }

    [Theory]
    [InlineData("01157")]
    [InlineData("01162")]
    [InlineData("01167")]
    [InlineData("01172")]
    [InlineData("01177")]
    [Rule("rr:nemesis-encounter-set.3")]
    public void ACompleteSetWithOnePrintedMinionNeedsNoParenthetical(string face)
    {
        // "An identity’s 'nemesis minion' is the minion belonging to that identity’s nemesis set."
        Assert.True(Cards.IsNemesisMinion(face));
    }

    [Theory]
    [InlineData("An ability mentions nemesis minion.")]
    [InlineData("(Other reminder.)")]
    [Rule("rr:nemesis-encounter-set.3.1")]
    public void MultipleMinionsWithoutPrintedDesignationRefuseToGuess(string text)
    {
        // Parenthetical designation is required for multiple printed minions.
        var catalog = Synthetic(text);
        Assert.Throws<RulesNotImplementedException>(() => catalog.IsNemesisMinion("a"));
        Assert.Throws<RulesNotImplementedException>(() => catalog.IsNemesisMinion("b"));
    }

    [Theory]
    [InlineData("(Example's nemesis minion.)")]
    [InlineData("(Example’s nemesis minion.)")]
    [Rule("rr:nemesis-encounter-set.3.1")]
    public void DesignationUsesParentheticalGrammarRatherThanIdentityOrCardNames(string text)
    {
        // "one or more" permits each explicitly designated printed minion.
        var catalog = Synthetic(text);
        Assert.True(catalog.IsNemesisMinion("a"));
        Assert.False(catalog.IsNemesisMinion("b"));
        var multiple = Synthetic(text, "(Another's nemesis minion.)");
        Assert.True(multiple.IsNemesisMinion("a"));
        Assert.True(multiple.IsNemesisMinion("b"));
    }

    private static CardCatalog Synthetic(string text, string otherText = "") => CardCatalog.Parse(
        System.Text.Json.JsonSerializer.Serialize(new { cards = new[] {
            new { card_id = "a", type = "Minion", pack = "synthetic", set = "synthetic_nemesis",
                attributes = new { Nemesis = "Synthetic" }, text_plain = text },
            new { card_id = "b", type = "Minion", pack = "synthetic", set = "synthetic_nemesis",
                attributes = new { Nemesis = "Synthetic" }, text_plain = otherText }
        }}));
}
