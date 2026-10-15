using Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CompactTableTypographyTests
{
    [Fact]
    public void LargeReadingScaleEnlargesEssentialTypeWithinTheSamePhysicalFace()
    {
        var features = new CardFaceFeatures(false, true, true, true, true, false, 0);
        var ordinary = new CardFaceRegions(new Vector2(164, 232), features);
        var large = new CardFaceRegions(new Vector2(164, 232), features with { ReadingScale = InterfaceScale.Percent150 });

        Assert.Equal(ordinary.TitleFontSize + 2, large.TitleFontSize);
        Assert.Equal(ordinary.Kind.Size.Y + 2, large.Kind.Size.Y);
        Assert.Equal(ordinary.Cost, large.Cost);
        Assert.Equal(ordinary.Health, large.Health);
        Assert.Equal(ordinary.RulesFontSize, large.RulesFontSize);
        Assert.True(large.Stats.End.Y <= large.Rules.Position.Y);
    }

    [Fact]
    public void LiveReadingScaleChangeCannotReuseTheSmallerNativeText()
    {
        var card = new BoardCardPresentation(1, 1, false, "Rhino", "", "VILLAIN", "", []);
        CardLayoutMetrics layout = CardControl.LayoutFor(card, CardDisplaySize.Board, InterfaceScale.Standard);
        var small = CardFaceReuseKey.From(card, CardDisplaySize.Board, InterfaceScale.Standard, layout);
        var large = CardFaceReuseKey.From(card, CardDisplaySize.Board, InterfaceScale.Standard,
            layout with { ReadingScale = InterfaceScale.Percent150 });
        Assert.NotEqual(small, large);
        Assert.Equal(small.Layout.Width, large.Layout.Width);
        Assert.Equal(small.Layout.MinimumHeight, large.Layout.MinimumHeight);
    }
}
