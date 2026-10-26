using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests;

public sealed class NemesisMinionIndependentTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [Rule("rr:nemesis-encounter-set.3")]
    public void PrintedSingletonRoleIgnoresPhysicalQuantityAndOtherProducts(int quantity)
    {
        // "An identity’s 'nemesis minion' is the minion belonging to that identity’s nemesis set."
        // These synthetic products deliberately reuse a set key and identity association.
        var catalog = CardCatalog.Parse(JsonSerializer.Serialize(new { cards = new[] {
            new { card_id = "singleton", type = "Minion", pack = "one", set = "same",
                quantity, attributes = new { Nemesis = "Example" }, text_plain = "" },
            new { card_id = "other-product", type = "Minion", pack = "two", set = "same",
                quantity = 1, attributes = new { Nemesis = "Example" }, text_plain = "" }
        }}));

        Assert.True(catalog.IsNemesisMinion("singleton"));
        Assert.True(catalog.IsNemesisMinion("other-product"));
    }

    [Fact]
    [Rule("rr:nemesis-encounter-set.3.1")]
    public void AnUnresolvedResearchSetDoesNotPreventUnrelatedFactsFromLoading()
    {
        // With multiple minions, the role "is designated by parenthetical text".
        // An unresolved synthetic set must fail on use without poisoning another set.
        var catalog = CardCatalog.Parse(JsonSerializer.Serialize(new { cards = new[] {
            new { card_id = "valid", type = "Minion", pack = "one", set = "valid",
                attributes = new { Nemesis = "Example" }, text_plain = "" },
            new { card_id = "ambiguous-a", type = "Minion", pack = "one", set = "unknown",
                attributes = new { Nemesis = "Unknown" }, text_plain = "" },
            new { card_id = "ambiguous-b", type = "Minion", pack = "one", set = "unknown",
                attributes = new { Nemesis = "Unknown" }, text_plain = "" }
        }}));

        Assert.Equal(3, catalog.Count);
        Assert.True(catalog.IsNemesisMinion("valid"));
        var failure = Assert.Throws<RulesNotImplementedException>(
            () => catalog.IsNemesisMinion("ambiguous-b"));
        Assert.Contains("unknown", failure.Message, StringComparison.Ordinal);
        Assert.True(catalog.IsNemesisMinion("valid"));
    }
}
