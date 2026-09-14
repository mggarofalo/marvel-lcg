using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DecisionPanelScaleTests
{
    [Theory]
    [InlineData(InterfaceScale.Compact, true, false, InterfaceScale.Standard)]
    [InlineData(InterfaceScale.Large, true, false, InterfaceScale.Large)]
    [InlineData(InterfaceScale.ExtraLarge, true, true, InterfaceScale.Standard)]
    [InlineData(InterfaceScale.Compact, false, false, InterfaceScale.Compact)]
    public void FixedTabletopDockMatchesItsThemeFloorWithoutCappingLargerControls(
        InterfaceScale requested,
        bool compactTableChrome,
        bool opening,
        InterfaceScale expected)
    {
        Assert.Equal(expected,
            DecisionPanel.EffectiveScale(requested, compactTableChrome, opening));
    }
}
