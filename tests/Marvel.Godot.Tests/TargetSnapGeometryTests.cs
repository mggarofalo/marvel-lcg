using Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TargetSnapGeometryTests
{
    private static readonly Rect2[] Slots = [new(0, 0, 100, 60), new(120, 0, 100, 60)];

    [Fact]
    public void NearEdgesAcquireButDistantDropsStayUnselected()
    {
        Assert.Equal(0, TargetSnapGeometry.Select(Slots, new Vector2(-7, 30), null));
        Assert.Null(TargetSnapGeometry.Select(Slots, new Vector2(-9, 30), null));
        Assert.Null(TargetSnapGeometry.Select([], new Vector2(20, 20), null));
    }

    [Fact]
    public void SmallGapRetainsDestinationWithoutOverridingDirectIntent()
    {
        Assert.Equal(0, TargetSnapGeometry.Select(Slots, new Vector2(110, 30), 0));
        Assert.Equal(1, TargetSnapGeometry.Select(Slots, new Vector2(113, 30), 0));
        Assert.Equal(1, TargetSnapGeometry.Select(Slots, new Vector2(122, 30), 0));
        Assert.Null(TargetSnapGeometry.Select(Slots, new Vector2(110, 30), null));
    }
}
