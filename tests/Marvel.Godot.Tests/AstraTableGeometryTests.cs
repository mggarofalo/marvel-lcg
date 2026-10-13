using Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class AstraTableGeometryTests
{
    [Theory]
    [InlineData(1060, 820)]
    [InlineData(1320, 962)]
    [InlineData(1670, 962)]
    public void TemporaryCausesAndSourceCountsNeverMovePhysicalRegions(float width, float height)
    {
        var plain = new AstraTableGeometry(width, height, false);
        var busy = plain with { HasRevealingCard = true, HasSourceTableau = true, HasAllies = true,
            PhysicalCardSize = new Vector2(400, 560), HasSeatSummaries = true };
        Assert.Equal(plain.Identity, busy.Identity);
        Assert.Equal(plain.Allies, busy.Allies);
        Assert.Equal(plain.SourceTableau, busy.SourceTableau);
        Assert.Equal(plain.Hand, busy.Hand);
        Assert.Equal(plain.MainScheme, busy.MainScheme);
        Assert.False(busy.Revealing.Intersects(busy.Identity));
        Assert.False(busy.Revealing.Intersects(busy.SourceTableau));
        Assert.False(busy.Hand.Intersects(busy.SourceTableau));
        Assert.False(busy.Hand.Intersects(busy.Context));
        Assert.True(busy.Hand.Size.X > width * 0.8f);
    }

    [Theory]
    [InlineData(820)]
    [InlineData(900)]
    [InlineData(962)]
    public void EntireRotatedHandClearsTheDecisionAndSources(float height)
    {
        var table = new AstraTableGeometry(1670, height, true);
        InterfaceScale scale = SpatialCardMetrics.TableScale(InterfaceScale.Percent150, height);
        CardLayoutMetrics metrics = VisualSystem.Card(CardDisplaySize.Hand, scale);
        var size = new Vector2(metrics.Width, metrics.MinimumHeight);
        foreach (int index in Enumerable.Range(0, 7))
        {
            SpatialCardPlacement card = table.HandCard(index, 7, size.X);
            var pose = new Transform2D(card.Rotation, card.Position + size / 2 - size.Rotated(card.Rotation) / 2);
            Rect2 face = SpatialCardFootprint.Bounds(pose, size);
            Assert.True(face.End.Y < table.Context.Position.Y);
            Assert.False(face.Intersects(table.SourceTableau));
        }
    }

    [Fact]
    public void DenseHandKeepsDistinctExposedPositionsAndStableZOrder()
    {
        var table = new AstraTableGeometry(1060, 820, false);
        SpatialCardPlacement[] cards = [.. Enumerable.Range(0, 12).Select(i => table.HandCard(i, 12, 156))];
        Assert.All(cards, card => Assert.True(card.Overlaps));
        Assert.Equal(Enumerable.Range(20, 12), cards.Select(card => card.ZIndex));
        Assert.True(cards[0].Rotation < 0);
        Assert.True(cards[^1].Rotation > 0);
        Assert.True(cards[^1].Position.X + 156 <= table.Hand.End.X + 0.01f);
        Assert.All(cards.Zip(cards.Skip(1)), pair => Assert.True(pair.Second.Position.X > pair.First.Position.X));
    }
}
