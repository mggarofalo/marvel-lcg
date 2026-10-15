using Marvel.Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DecisionTargetLabelTests
{
    [Fact]
    public void EffectShownInTheFixedHeaderIsNotRepeatedInTheTargetSelector() =>
        Assert.Equal("Rhino", DecisionDraftRenderer.TargetLabel("Rhino",
            "Rhino: Printed damage: 8; later effects unresolved.",
            "Printed damage: 8; later effects unresolved."));

    [Theory]
    [InlineData("Rhino", "Rhino: Printed damage: 8; later effects unresolved.", "Rhino: Printed damage: 8; later effects unresolved.")]
    [InlineData("Rhino", "Printed damage: 8; later effects unresolved.", "Rhino · Printed damage: 8; later effects unresolved.")]
    [InlineData("Rhino", null, "Rhino")]
    public void KeepsIdentityAndCompleteEngineQualificationWithoutRepeatingTheName(string name, string? detail, string expected) =>
        Assert.Equal(expected, DecisionDraftRenderer.TargetLabel(name, detail));
}
