using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DecisionPanelScaleTests
{
    [Theory]
    [InlineData(InterfaceScale.Compact, true, false, InterfaceScale.Compact)]
    [InlineData(InterfaceScale.Large, true, false, InterfaceScale.Large)]
    [InlineData(InterfaceScale.ExtraLarge, true, true, InterfaceScale.ExtraLarge)]
    [InlineData(InterfaceScale.Compact, false, false, InterfaceScale.Compact)]
    [InlineData(InterfaceScale.ExtraLarge, false, true, InterfaceScale.ExtraLarge)]
    public void PromptEditorsHonorRequestedReadingScaleAtEveryTableLayout(
        InterfaceScale requested,
        bool compactTableChrome,
        bool opening,
        InterfaceScale expected)
    {
        Assert.Equal(expected,
            DecisionPanel.EffectiveScale(requested, compactTableChrome, opening));
    }
}
