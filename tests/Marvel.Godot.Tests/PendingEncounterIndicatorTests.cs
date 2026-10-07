using Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PendingEncounterIndicatorTests
{
    [Fact]
    public void WaitingEncounterCardsAreNotHiddenUnderOtherPiles()
    {
        var area = new BoardAreaPresentation(7, "Dealt encounters", "", [
            new BoardCardPresentation(null, 2, true, "", "", "", "", [])], [])
        {
            Zone = "DealtEncounterCardsDeck", Seat = 0, Prominence = BoardAreaProminence.Live,
        };
        Assert.Empty(SpatialTableObjectRenderer.Unplaced([area]));
    }

    [Theory]
    [InlineData(1060, 820)]
    [InlineData(1320, 962)]
    [InlineData(1670, 962)]
    public void WaitingCardsHaveSpaceOutsidePilesHandAndDecision(int width, int height)
    {
        var table = new AstraTableGeometry(width, height, LargeText: true);
        Rect2 waiting = table.PendingEncounters;
        Assert.False(waiting.Intersects(table.PlayerDeck));
        Assert.False(waiting.Intersects(table.PlayerDiscard));
        Assert.False(waiting.Intersects(table.Overflow));
        Assert.False(waiting.Intersects(table.Hand));
        Assert.False(waiting.Intersects(table.Context));
        Assert.True(new Rect2(0, 0, width, height).Encloses(waiting));
    }

    [Fact]
    public void EmptyDealtAreasDoNotAnnounceAWaitingCard()
    {
        Assert.Empty(PendingEncounterIndicator.Compact(0));
        Assert.Empty(PendingEncounterIndicator.Description(0, 0));
        Assert.Equal("Player 2: 3 encounter cards waiting to reveal", PendingEncounterIndicator.Description(3, 1));
        Assert.Contains("3", PendingEncounterIndicator.Compact(3));
    }
}
