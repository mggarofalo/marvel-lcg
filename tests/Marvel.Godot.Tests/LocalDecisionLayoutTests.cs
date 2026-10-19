using Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class LocalDecisionLayoutTests
{
    [Theory]
    [InlineData(40, 300)]
    [InlineData(1600, 300)]
    [InlineData(900, 750)]
    public void FrameFitsViewportWithoutCoveringHand(float sourceX, float sourceY)
    {
        Rect2 frame = LocalDecisionLayout.Fit(new(1920, 1080), new(sourceX, sourceY, 160, 220), 740, 540);
        Assert.InRange(frame.Position.X, 16, 1920 - frame.Size.X - 16);
        Assert.InRange(frame.Position.Y, 72, 728 - frame.Size.Y);
        Assert.True(frame.End.Y < 740);
        Assert.True(frame.Size.X >= 500);
    }
}
