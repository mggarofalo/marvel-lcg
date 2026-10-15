using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ButtonMaterialTests
{
    [Fact]
    public void CommitmentAndChoiceLabelsStayReadableAcrossSurfaceStates()
    {
        Assert.All(new[] { ButtonMaterials.Primary, ButtonMaterials.PrimaryHover, ButtonMaterials.PrimaryPressed },
            fill => Assert.True(VisualSystem.ContrastRatio(VisualSystem.Palette.OnAccent, fill) >= 4.5));
        Assert.All(new[] { ButtonMaterials.Secondary, ButtonMaterials.SecondaryHover,
            ButtonMaterials.SecondaryPressed, ButtonMaterials.SelectedSurface },
            fill => Assert.True(VisualSystem.ContrastRatio(VisualSystem.Palette.Text, fill) >= 4.5));
        Assert.True(VisualSystem.ContrastRatio(VisualSystem.Palette.Unavailable,
            ButtonMaterials.UnavailableSurface) >= 4.5);
        Assert.True(VisualSystem.ContrastRatio(VisualSystem.Palette.OnAccent, ButtonMaterials.Primary) >= 3,
            "The inset keyboard-focus ring must remain visible on amber.");
        Assert.NotEqual(ButtonMaterials.Primary, VisualSystem.For(InteractiveVisualState.Selected).Background);
    }

    [Fact]
    public void InteractionKeepsCaptionAndHitGeometryFixed()
    {
        Assert.All(Enum.GetValues<InteractiveVisualState>(), state =>
            Assert.Equal(0, VisualSystem.For(state).VerticalOffset));
        Assert.Equal(6, VisualSystem.Controls(InterfaceScale.Percent100).CornerRadius);
        Assert.Equal(9, VisualSystem.Controls(InterfaceScale.Percent150).CornerRadius);
    }

    [Theory]
    [InlineData("ToggleHistory")]
    [InlineData("CopyInvitation")]
    [InlineData("CopyReport")]
    [InlineData("SaveReport")]
    [InlineData("Synchronize")]
    [InlineData("CompleteChoiceSheet")]
    public void UtilitiesRemainQuietEvenWhenAnAuthoredControlDeclaresPrimary(string name) =>
        Assert.Equal(GodotThemeVariations.TertiaryButton,
            ButtonHierarchy.Variation(name, GodotThemeVariations.PrimaryButton));

    [Fact]
    public void EndingATurnRemainsASecondaryChoiceAndUnknownOffersKeepTheirDeclaredRole()
    {
        Assert.Equal(GodotThemeVariations.ChoiceButton,
            ButtonHierarchy.Variation("ContextualDecline", GodotThemeVariations.PrimaryButton));
        Assert.Equal(GodotThemeVariations.LegalTargetButton,
            ButtonHierarchy.Variation("Card51Target", GodotThemeVariations.LegalTargetButton));
        Assert.Equal(GodotThemeVariations.PrimaryButton,
            ButtonHierarchy.Variation("ContextualCommit", GodotThemeVariations.PrimaryButton));
    }
}
