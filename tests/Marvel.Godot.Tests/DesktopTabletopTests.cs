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
}
