using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardGeometryTests
{
    [Theory]
    [InlineData("HERO", GodotThemeVariations.IdentityCard)]
    [InlineData("ALLY", GodotThemeVariations.PlayerCard)]
    [InlineData("MINION", GodotThemeVariations.EnemyCard)]
    [InlineData("MAIN SCHEME", GodotThemeVariations.SchemeCard)]
    [InlineData("SIDE SCHEME", GodotThemeVariations.SchemeCard)]
    public void CompactCardsRetainTheirSemanticFrame(string kind, string expected)
    {
        BoardCardPresentation card = Card(kind);

        Assert.Equal(expected, CardControl.VariationFor(card, CardDisplaySize.Board));
        Assert.Equal(expected, CardControl.VariationFor(card, CardDisplaySize.Hand));
    }

    [Fact]
    public void TableSchemesUseTheirPrintedLandscapeGeometry()
    {
        CardLayoutMetrics scheme = CardControl.LayoutFor(
            Card("MAIN SCHEME"), CardDisplaySize.Board, InterfaceScale.Standard);
        CardLayoutMetrics eventCard = CardControl.LayoutFor(
            Card("EVENT"), CardDisplaySize.Board, InterfaceScale.Standard);

        Assert.True(scheme.Width > scheme.MinimumHeight);
        Assert.True(eventCard.Width < eventCard.MinimumHeight);
        Assert.Equal(eventCard.Width, scheme.MinimumHeight);
        Assert.Equal(eventCard.MinimumHeight, scheme.Width);
    }

    private static BoardCardPresentation Card(string kind) => new(
        1, 1, false, "Test card", string.Empty, kind, string.Empty, []);
}
