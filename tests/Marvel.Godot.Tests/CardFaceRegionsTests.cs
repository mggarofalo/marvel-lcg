using Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardFaceRegionsTests
{
    [Theory]
    [InlineData(172, 240, false)]
    [InlineData(264, 369, false)]
    [InlineData(400, 560, true)]
    public void PrintedIconsDoNotMoveTheStatOrCornerAnchors(int width, int height, bool full)
    {
        var features = new CardFaceFeatures(false, true, false, true, true, full, 0);
        var plain = new CardFaceRegions(new Vector2(width - 8, height - 8), features);
        var statuses = new CardFaceRegions(new Vector2(width - 8, height - 8), features with { PrintedIconRows = 2 });

        Assert.Equal(plain.Stats, statuses.Stats);
        Assert.Equal(plain.Cost, statuses.Cost);
        Assert.Equal(plain.Health, statuses.Health);
        Assert.Equal(plain.Resources, statuses.Resources);
        Assert.False(statuses.Tokens.Intersects(statuses.Stats));
        Assert.True(statuses.Rules.Position.Y >= statuses.Tokens.End.Y);
        Assert.True(statuses.Rules.Size.Y > 0);
    }

    [Fact]
    public void NoArtUsesTheBodyForRulesAndEmptyTraitsDoNotReserveALine()
    {
        var features = new CardFaceFeatures(false, false, false, false, false, true, 0);
        var plain = new CardFaceRegions(new Vector2(392, 552), features);
        var art = new CardFaceRegions(new Vector2(392, 552), features with { HasArt = true });
        var traits = new CardFaceRegions(new Vector2(392, 552), features with { HasTraits = true });

        Assert.Equal(Vector2.Zero, plain.Illustration.Size);
        Assert.True(plain.Rules.Position.Y < 552 / 3f);
        Assert.True(plain.Rules.Size.Y > art.Rules.Size.Y);
        Assert.True(plain.Rules.Size.Y > traits.Rules.Size.Y);
    }

    [Fact]
    public void IllustratedInspectorKeepsRulesAndPrintedIconsOutOfTheStatRail()
    {
        var regions = new CardFaceRegions(new Vector2(392, 552),
            new CardFaceFeatures(false, false, true, true, true, true, 2));

        Assert.True(regions.Illustration.Size.Y > 0);
        Assert.False(regions.Stats.Intersects(regions.Rules));
        Assert.False(regions.Stats.Intersects(regions.Tokens));
        Assert.False(regions.Stats.Intersects(regions.Traits));
        Assert.True(regions.Rules.Size.Y > 0);
    }

    [Theory]
    [InlineData(164, 232, false)]
    [InlineData(392, 552, true)]
    public void ResourceHitFieldDoesNotOverlapHealth(int width, int height, bool full)
    {
        var regions = new CardFaceRegions(new Vector2(width, height),
            new CardFaceFeatures(false, true, true, true, true, full, 0) { HasProgress = true });
        Assert.False(regions.Resources.Intersects(regions.Health));
        Assert.True(regions.Resources.Size.X > 0);
        Assert.True(regions.Resources.End.X < regions.Health.Position.X);
    }

    [Fact]
    public void ConcealedCardKeepsAPortraitFootprintWithoutConsultingHiddenIdentity()
    {
        var card = new BoardCardPresentation(null, 1, true, "Face-down encounter card", "", "CONCEALED CARD", "", []);
        CardLayoutMetrics layout = CardControl.LayoutFor(card, CardDisplaySize.Board, InterfaceScale.Percent100);
        Vector2 size = SpatialCardMetrics.FaceSize(card, CardDisplaySize.Board, layout, InterfaceScale.Percent100);

        Assert.Equal(new Vector2(172, 240), size);
        Assert.Equal(size, SpatialCardMetrics.FaceSize(card with { Title = "Other generic back" },
            CardDisplaySize.Board, layout, InterfaceScale.Percent100));
    }
}
