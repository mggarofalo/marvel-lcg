using Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class AstraTableGeometryTests
{
    [Fact]
    public void FarSideOppositionPrecedesEngagementIdentityAssetsAndHand()
    {
        var table = new AstraTableGeometry(1320, 930, LargeText: false);

        Assert.True(table.Villain.End.Y < table.EngagedEnemies.Position.Y);
        Assert.True(table.EngagedEnemies.End.Y < table.Identity.Position.Y);
        Assert.True(table.Identity.End.Y <= table.Assets.Position.Y + 24);
        Assert.True(table.Assets.End.Y <= table.Hand.End.Y);
        Assert.True(table.VillainMat.End.Y < table.PlayerMat.Position.Y);
    }

    [Fact]
    public void SixCardHandOverlapsFansAndRaisesInStableZOrder()
    {
        var table = new AstraTableGeometry(1320, 930, LargeText: false);
        SpatialCardPlacement[] cards = [.. Enumerable.Range(0, 6)
            .Select(index => table.HandCard(index, 6, 156))];

        Assert.All(cards, card => Assert.True(card.Overlaps));
        Assert.True(cards[0].Rotation < 0);
        Assert.True(cards[^1].Rotation > 0);
        Assert.True(cards[2].Position.Y < cards[0].Position.Y);
        Assert.Equal(Enumerable.Range(20, 6), cards.Select(card => card.ZIndex));
        Assert.All(cards.Zip(cards.Skip(1)), pair =>
            Assert.True(pair.Second.Position.X - pair.First.Position.X < 156));
    }

    [Fact]
    public void LargeTextProfileKeepsTheSamePhysicalTableGrammar()
    {
        var standard = new AstraTableGeometry(1320, 930, LargeText: false);
        var large = new AstraTableGeometry(1320, 930, LargeText: true);

        Assert.Equal(standard.Villain, large.Villain);
        Assert.Equal(standard.Identity, large.Identity);
        Assert.Equal(standard.Hand, large.Hand);
        Assert.True(large.LargeText);
    }

    [Fact]
    public void DenseRowsUseBoundedOverlapInsteadOfEscapingTheirRegion()
    {
        var table = new AstraTableGeometry(1320, 930, LargeText: false);
        Rect2 region = table.Allies;
        Vector2 card = new(156, 176);
        Vector2[] positions = [.. Enumerable.Range(0, 5)
            .Select(index => table.Slot(region, index, 5, card))];

        Assert.All(positions, position => Assert.InRange(
            position.X + card.X,
            region.Position.X + card.X,
            region.End.X + 0.01f));
        Assert.True(positions[1].X - positions[0].X < card.X);
    }
}
