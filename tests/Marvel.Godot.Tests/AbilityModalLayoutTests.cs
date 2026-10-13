using Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class AbilityModalLayoutTests
{
    [Theory]
    [InlineData(1920, 1080, 750, 430, 250)]
    [InlineData(1920, 1080, 754, 434, 600)]
    [InlineData(1600, 900, 650, 370, 300)]
    [InlineData(1040, 680, 470, 285, 300)]
    public void SheetLeavesTheHandChronologyAndHistoryEdgeUncovered(
        float width, float height, float hand, float chronology, float contentHeight)
    {
        Rect2 frame = AbilityModalLayout.Fit(new Vector2(width, height), hand, chronology, contentHeight);
        Assert.True(frame.Position.Y >= chronology + 12);
        Assert.True(frame.End.Y <= hand - 12);
        Assert.True(frame.End.X <= width - 56);
        Assert.True(frame.Position.X >= 12);
        Assert.InRange(frame.Size.Y, 120, contentHeight);
        Assert.False(frame.Intersects(new Rect2(0, hand, width, height - hand)));
        Assert.False(frame.Intersects(new Rect2(width - 44, 88, 44, 44)));
    }
}
