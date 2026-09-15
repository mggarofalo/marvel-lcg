using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DecisionPanelScaleTests
{
    [Theory]
    [InlineData(InterfaceScale.Compact, true, false, InterfaceScale.Standard)]
    [InlineData(InterfaceScale.Large, true, false, InterfaceScale.Standard)]
    [InlineData(InterfaceScale.ExtraLarge, true, true, InterfaceScale.Standard)]
    [InlineData(InterfaceScale.Compact, false, false, InterfaceScale.Compact)]
    [InlineData(InterfaceScale.ExtraLarge, false, true, InterfaceScale.ExtraLarge)]
    public void FixedTabletopDockKeepsStableDesktopControlGeometry(
        InterfaceScale requested,
        bool compactTableChrome,
        bool opening,
        InterfaceScale expected)
    {
        Assert.Equal(expected,
            DecisionPanel.EffectiveScale(requested, compactTableChrome, opening));
    }
}
