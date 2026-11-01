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
    [InlineData(1320, 962, false)]
    [InlineData(1670, 962, true)]
    public void AColumnTablesSeamHoldsReferencesTheSituationAndActivityAboveTheTableau(float width, float height, bool encounterGroups)
    {
        var table = new AstraTableGeometry(width, height, true, HasRevealingCard: true, HasSourceTableau: true)
        {
            HasEncounterGroups = encounterGroups, HasGameAreas = !encounterGroups,
        };
        var situation = new Rect2(table.Revealing.End.X, table.Revealing.Position.Y,
            table.SourceTableau.Position.X - table.Revealing.End.X, table.Revealing.Size.Y);
        Assert.True(table.Revealing.Position.Y >= table.VillainMat.End.Y);
        Assert.False(situation.Intersects(table.ColumnActivity));
        Assert.True(table.SourceTableau.Position.Y >= table.ColumnActivity.End.Y);
        Assert.False(table.Revealing.Intersects(table.Identity));
    }

    [Theory]
    [InlineData(820)]
    [InlineData(900)]
    [InlineData(937)]
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

    [Fact]
    public void LargerChromeRetainsReadablePhysicalCardSize()
    {
        var scale = SpatialCardMetrics.TableScale(InterfaceScale.Percent150, 937);
        Assert.True(VisualSystem.Card(CardDisplaySize.Board, scale).Width >= 156);
    }

    [Theory]
    [InlineData(1864, 990, InterfaceScale.Percent100)]
    [InlineData(1864, 990, InterfaceScale.Percent150)]
    [InlineData(2504, 1330, InterfaceScale.Percent100)]
    public void ASeparatedTableLaysItsFarSideOnTheGrid(float width, float height, InterfaceScale scale)
    {
        var table = new AstraTableGeometry(width, height, scale >= InterfaceScale.Percent130) { HasGameAreas = true, ReadingScale = scale };
        TableGrid grid = TableGrid.For(width, scale);
        Assert.Equal(grid.Span(2, 8), (table.GameAreaBand.Position.X, table.GameAreaBand.Size.X));
        Assert.Equal(grid.Span(10, 2), (table.EngagedEnemies.Position.X, table.EngagedEnemies.Size.X));
        Assert.True(table.SeatStrip.End.X <= table.GameAreaBand.Position.X);
        Assert.True(table.GameAreaBand.End.X <= table.EngagedEnemies.Position.X);
        Assert.True(table.VillainMat.Encloses(table.GameAreaBand));
        Assert.True(table.VillainMat.Encloses(table.EngagedEnemies));
    }

    [Fact]
    public void AWholeTableKeepsItsReferenceRegions()
    {
        var table = new AstraTableGeometry(1320, 962, false) { ReadingScale = InterfaceScale.Percent150 };
        Assert.Equal(new Rect2(944, 34, 354, 248), table.EngagedEnemies);
        Assert.Equal(new Rect2(22, 214, 158, 164), table.SeatStrip);
    }
}
