using Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class AstraTableGeometryTests
{
    [Theory]
    [InlineData(80)]
    [InlineData(100)]
    public void EngagedCardsReserveTheirFaceAndUprightStateBeforeSupports(int percent)
    {
        CardLayoutMetrics face = VisualSystem.Card(CardDisplaySize.Board, (InterfaceScale)percent);
        var size = new Vector2(face.Width, face.MinimumHeight);
        var table = new AstraTableGeometry(1320, 962, false, PhysicalCardSize: size);
        Assert.True(table.EngagedEnemies.Size.X >= SpatialCardFootprint.OccupiedSize(size).X);
        Assert.True(table.Assets.Position.X > table.EngagedEnemies.End.X);
    }

    [Theory]
    [InlineData(1320, 962, 224)]
    [InlineData(1100, 820, 240)]
    public void PhysicalInstalledFacesLeaveSpaceAboveTheRotatedHand(float width, float height, float faceHeight)
    {
        var table = new AstraTableGeometry(width, height, LargeText: true,
            PhysicalCardSize: new Vector2(194, faceHeight));

        Assert.True(table.Hand.Position.Y - 12 > table.Identity.Position.Y + faceHeight);
        Assert.True(table.Hand.End.Y < table.Context.Position.Y);
    }

    [Fact]
    public void OppositionStaysFarWhileSupportsFlankTheIdentityAndHandStaysNear()
    {
        var table = new AstraTableGeometry(1320, 930, LargeText: false);

        Assert.True(table.Villain.End.Y < table.EngagedEnemies.Position.Y);
        Assert.True(table.Villain.End.Y < table.Identity.Position.Y);
        Assert.True(table.EngagedEnemies.End.X < table.Assets.Position.X);
        Assert.True(table.Assets.End.X < table.Identity.Position.X);
        Assert.True(table.Allies.Position.X > table.Identity.End.X);
        Assert.True(table.Identity.End.Y < table.Hand.Position.Y);
        Assert.True(table.Assets.End.Y < table.Hand.Position.Y);
        Assert.True(table.Hand.End.Y < table.Context.Position.Y);
        Assert.False(table.Upgrades.Intersects(table.Hand));
    }

    [Theory]
    [InlineData(1320, 962)]
    [InlineData(1670, 962)]
    public void RevealingCardKeepsItsOwnIdentitySpaceAndUsesTheDenseSupportFallback(float width, float height)
    {
        var table = new AstraTableGeometry(width, height, LargeText: true, HasRevealingCard: true);

        if (table.HasSeparateRevealSlot) Assert.False(table.Revealing.Intersects(table.Assets));
        else Assert.Equal(table.Assets, table.Revealing);
        Assert.False(table.Revealing.Intersects(table.Identity));
        Assert.False(table.Revealing.Intersects(table.Allies));
        Assert.False(table.Revealing.Intersects(table.Hand));
        Assert.True(table.Revealing.End.X < table.Identity.Position.X);
        Assert.True(table.Allies.Position.X > table.Identity.End.X);
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
        Assert.True(cards[2].Position.Y > cards[0].Position.Y);
        Assert.Equal(Enumerable.Range(20, 6), cards.Select(card => card.ZIndex));
        Assert.All(cards.Zip(cards.Skip(1)), pair =>
            Assert.True(pair.Second.Position.X - pair.First.Position.X < 156));
    }

    [Theory]
    [InlineData(1100, 780)]
    [InlineData(1320, 962)]
    public void HandFanUsesItsAvailableWidthAndLeavesTheTaskDockUncovered(float width, float height)
    {
        var table = new AstraTableGeometry(width, height, LargeText: false);
        const float cardWidth = 156;
        SpatialCardPlacement[] cards = [.. Enumerable.Range(0, 6)
            .Select(index => table.HandCard(index, 6, cardWidth))];

        Assert.InRange(cards[0].Position.X, table.Hand.Position.X, table.Hand.Position.X + 0.01f);
        Assert.InRange(cards[^1].Position.X + cardWidth, table.Hand.End.X - 0.01f, table.Hand.End.X + 0.01f);
        Assert.All(cards, card => Assert.True(card.Position.Y + table.Hand.Size.Y < table.Context.Position.Y + 24));
    }

    [Fact]
    public void PrintedPortraitsAndTheirRotatedFanLeaveTheDecisionVisible()
    {
        foreach (float height in new[] { 820f, 900f, 962f })
        {
            InterfaceScale scale = SpatialCardMetrics.TableScale(InterfaceScale.Percent150, height);
            CardLayoutMetrics board = VisualSystem.Card(CardDisplaySize.Board, scale);
            CardLayoutMetrics hand = VisualSystem.Card(CardDisplaySize.Hand, scale);
            var table = new AstraTableGeometry(1320, height, true,
                PhysicalCardSize: new Vector2(board.Width, board.MinimumHeight));
            foreach (int index in Enumerable.Range(0, 6))
            {
                SpatialCardPlacement card = table.HandCard(index, 6, hand.Width);
                float extent = Math.Abs(MathF.Sin(card.Rotation)) * hand.Width / 2
                    + Math.Abs(MathF.Cos(card.Rotation)) * hand.MinimumHeight / 2;
                Assert.True(card.Position.Y + hand.MinimumHeight / 2 + extent < table.Context.Position.Y,
                    $"Hand card {index} overlaps the decision at table height {height}");
            }
        }
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
