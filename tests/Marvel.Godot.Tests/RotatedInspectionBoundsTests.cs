using Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class RotatedInspectionBoundsTests
{
    [Fact]
    public void ExhaustedHostReservesItsActualQuarterTurnedFootprintForInspection()
    {
        Rect2 bounds = SpatialCardFootprint.Bounds(new Transform2D(MathF.PI / 2, new Vector2(500, 100)), new Vector2(172, 240));
        Assert.InRange(bounds.Position.X, 259.99f, 260.01f);
        Assert.InRange(bounds.Position.Y, 99.99f, 100.01f);
        Assert.InRange(bounds.Size.X, 239.99f, 240.01f);
        Assert.InRange(bounds.Size.Y, 171.99f, 172.01f);
        Rect2 preview = CardInspectorPlacement.Fit(new Vector2(1920, 1080), bounds, new Vector2(850, 900));
        Assert.False(preview.Intersects(bounds));
        Assert.True(new Rect2(0, 0, 1920, 1080).Encloses(preview));
    }

    [Theory]
    [InlineData(-7)]
    [InlineData(7)]
    public void AHandCardsTiltedCornersRemainOutsideTheChosenPreview(float degrees)
    {
        Transform2D pose = new(Mathf.DegToRad(degrees), new Vector2(800, 650));
        Rect2 bounds = SpatialCardFootprint.Bounds(pose, new Vector2(172, 240));
        Assert.True(bounds.Size.X > 172);
        Assert.True(bounds.Size.Y > 240);
        Rect2 preview = CardInspectorPlacement.Fit(new Vector2(1920, 1080), bounds, new Vector2(650, 600));
        Assert.False(preview.Intersects(bounds));
    }
}
