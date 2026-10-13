using Marvel.Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

[Collection("Source presentation state")]
public sealed class SourceVacancyOrderTests
{
    [Fact]
    public void EmptyPlacementSurvivesRenderAndArrivalsFillVacanciesWithoutMovingExistingCards()
    {
        SourcePresentationOrder.Store(911, [null, 2, null, null, 1]);
        Assert.Equal([null, 2, null, null, 1, null], Ids(911, [Card(1), Card(2)], 6));
        Assert.Equal([3, 2, null, null, 1, null], Ids(911, [Card(1), Card(2), Card(3)], 6));
        Assert.Equal([3, null, null, null, 1, null], Ids(911, [Card(1), Card(3)], 6));
        Assert.Equal([3, 4, null, null, 1, null], Ids(911, [Card(4), Card(3), Card(1)], 6));
    }

    [Fact]
    public void GridKeepsOverflowPlacementAndDuplicateSourcesRemainSeparatePhysicalCards()
    {
        SourcePresentationOrder.Store(912, [1, null, null, null, null, null, null, 2]);
        Assert.Equal([1, null, null, null, null, null, null, 2], Ids(912, [Card(2), Card(1)], 3));
        SourcePresentationOrder.Store(913, [2, 1]);
        Assert.Equal([2, 1, null], Ids(913, [Card(1), Card(2)], 3));
        Assert.Equal(2, SourcePresentationOrder.Arrange(912, [Card(1), Card(2)]).Length);
    }

    [Fact]
    public void SessionResetClearsVacanciesAndSeatOrdering()
    {
        SourcePresentationOrder.Store(914, [null, null, 1]);
        SourcePresentationOrder.Clear();
        Assert.Equal([1, 2, null], Ids(914, [Card(1), Card(2)], 3));
    }

    [Theory]
    [InlineData(750, 216, 9, 4, 12)]
    [InlineData(750, 216, 12, 4, 12)]
    [InlineData(400, 216, 9, 2, 10)]
    public void SlotCapacityFollowsVisibleRowsAndResponsiveColumns(float width, float height,
        int count, int columns, int capacity)
    {
        var layout = new SourceTableauLayout(width, height, count);
        Assert.Equal(columns, layout.Columns);
        Assert.Equal(capacity, layout.Capacity);
    }

    [Fact]
    public void SixteenSourcesUseFourRowsWithoutInventingATrailingOverflowGap()
    {
        var layout = new SourceTableauLayout(620, 258, 16);
        Assert.Equal(4, layout.Columns);
        Assert.Equal(16, layout.Capacity);
        Assert.Equal(258, layout.ContentHeight(layout.Capacity));
        Assert.Equal(258, layout.Position(15).Y + SourceTableauLayout.TileHeight);
        var overflow = new SourceTableauLayout(620, 258, 17);
        Assert.True(overflow.ContentHeight(overflow.Capacity) > overflow.Height);
    }

    private static int?[] Ids(int seat, BoardCardPresentation[] cards, int capacity) =>
        SourcePresentationOrder.Slots(seat, cards, capacity).Select(card => card?.TargetId).ToArray();

    private static BoardCardPresentation Card(int id) => new(id, 1, false, "Same title", "", "", "", []);

    [Fact]
    public void NewSourcesNeverChangePreviouslyArrangedGridCoordinates()
    {
        var sparse = new SourceTableauLayout(660, 288, 4);
        foreach (int count in new[] { 9, 11, 12, 16 })
        {
            var grown = new SourceTableauLayout(660, 288, count);
            Assert.Equal(sparse.Columns, grown.Columns);
            Assert.Equal(sparse.TileWidth, grown.TileWidth);
            Assert.Equal(sparse.Position(7), grown.Position(7));
        }
    }
}
