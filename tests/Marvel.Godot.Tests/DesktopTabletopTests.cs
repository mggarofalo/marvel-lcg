using Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DesktopTabletopTests
{
    [Fact]
    public void FixedWorkspaceCoversTheDesktopProfileButNotCompactDiagnostics()
    {
        Assert.True(DesktopTabletop.Uses(new Vector2(1920, 1080)));
        Assert.False(DesktopTabletop.Uses(new Vector2(1919, 1080)));
        Assert.False(DesktopTabletop.Uses(new Vector2(1920, 1079)));
        Assert.False(DesktopTabletop.Uses(new Vector2(1800, 900)));
        Assert.False(DesktopTabletop.Uses(new Vector2(1280, 720)));
    }

    [Theory]
    [InlineData(true, 1919, 1080)]
    [InlineData(true, 1920, 1079)]
    [InlineData(false, 1920, 1080)]
    public void RenderedRouteChangesAtEitherExactDesktopBoundary(
        bool renderedDesktopTabletop,
        int width,
        int height)
    {
        Assert.True(DesktopTabletop.RouteChanged(
            renderedDesktopTabletop, new Vector2(width, height)));
    }

    [Fact]
    public void UnknownRenderedRouteDoesNotRequestASecondInitialRender()
    {
        Assert.False(DesktopTabletop.RouteChanged(null, new Vector2(1920, 1080)));
    }

    [Theory]
    [InlineData(true, 1920, 1080)]
    [InlineData(false, 1919, 1080)]
    [InlineData(false, 1920, 1079)]
    public void RenderedRouteRemainsStableOnTheSameSideOfTheBoundary(
        bool renderedDesktopTabletop,
        int width,
        int height)
    {
        Assert.False(DesktopTabletop.RouteChanged(
            renderedDesktopTabletop, new Vector2(width, height)));
    }
}
