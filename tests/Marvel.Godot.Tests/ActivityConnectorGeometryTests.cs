using Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ActivityConnectorGeometryTests
{
    [Fact]
    public void CrossingCopyLeavesTwoVisibleEnds()
    {
        var pieces = ActivityConnectorGeometry.Segments(new(0, 0), new(100, 100), [new Rect2(40, 30, 20, 40)]);
        Assert.Equal(2, pieces.Count);
        Assert.True(new Vector2(40, 40).IsEqualApprox(pieces[0].To));
        Assert.True(new Vector2(60, 60).IsEqualApprox(pieces[1].From));
    }

    [Fact]
    public void UnobstructedLineRemainsWhole()
    {
        var pieces = ActivityConnectorGeometry.Segments(new(0, 0), new(100, 0), [new Rect2(40, 30, 20, 40)]);
        Assert.Equal((new Vector2(0, 0), new Vector2(100, 0)), Assert.Single(pieces));
    }

    [Fact]
    public void FullyCoveredLineIsAbsent()
    {
        Assert.Empty(ActivityConnectorGeometry.Segments(new(20, 20), new(30, 30), [new Rect2(0, 0, 100, 100)]));
    }
    [Fact]
    public void OverlappingCopyRegionsLeaveOnlyTheExposedEndsInDrawingOrder()
    {
        var pieces = ActivityConnectorGeometry.Segments(new(100, 50), new(0, 50),
            [new Rect2(30, 20, 40, 60), new Rect2(50, 10, 40, 80)]);
        Assert.Equal(2, pieces.Count);
        Assert.Equal((new Vector2(100, 50), new Vector2(90, 50)), pieces[0]);
        Assert.Equal((new Vector2(30, 50), new Vector2(0, 50)), pieces[1]);
    }

    [Fact]
    public void CoveredArrowheadTipRetainsItsExposedWing()
    {
        var pieces = ActivityConnectorGeometry.Segments(new(50, 50), new(38, 57), [new Rect2(45, 40, 20, 20)]);
        var piece = Assert.Single(pieces);
        Assert.Equal(45, piece.From.X);
        Assert.Equal(new Vector2(38, 57), piece.To);
        Assert.True(piece.From.Y > 50);
    }

    [Fact]
    public void NeighboringCardFaceOccludesTheTraceButKeepsExteriorEndpoints()
    {
        var pieces = ActivityConnectorGeometry.Segments(new(45, 50), new(173, 50),
            [new Rect2(0, 20, 40, 60).Grow(2), new Rect2(80, 0, 40, 100).Grow(2),
             new Rect2(180, 20, 40, 60).Grow(2)]);
        Assert.Equal(2, pieces.Count);
        Assert.Equal((new Vector2(45, 50), new Vector2(78, 50)), pieces[0]);
        Assert.Equal((new Vector2(122, 50), new Vector2(173, 50)), pieces[1]);
    }

}
