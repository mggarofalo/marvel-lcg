using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DecisionPanelScaleTests
{
    [Theory]
    [InlineData(InterfaceScale.Compact, true, InterfaceScale.Standard)]
    [InlineData(InterfaceScale.ExtraLarge, true, InterfaceScale.Standard)]
    [InlineData(InterfaceScale.Compact, false, InterfaceScale.Compact)]
    [InlineData(InterfaceScale.ExtraLarge, false, InterfaceScale.ExtraLarge)]
    public void FixedTabletopDockUsesTheThemeScaleForEveryPrompt(
        InterfaceScale requested,
        bool compactTableChrome,
        InterfaceScale expected)
    {
        Assert.Equal(expected, DecisionPanel.EffectiveScale(requested, compactTableChrome));
    }
}
