using Xunit;

namespace Marvel.Godot.Tests;

public sealed class VisualSystemTests
{
    [Fact]
    public void TextAndInteractiveLabelsMeetReadableContrast()
    {
        VisualPalette palette = VisualSystem.Palette;
        (VisualColor Foreground, VisualColor Background)[] pairs =
        [
            (palette.Text, palette.Canvas),
            (palette.Text, palette.Surface),
            (palette.Text, palette.RaisedSurface),
            (palette.MutedText, palette.Canvas),
            (palette.MutedText, palette.Surface),
        ];

        Assert.All(pairs, pair => Assert.True(
            VisualSystem.ContrastRatio(pair.Foreground, pair.Background) >= 4.5,
            $"{pair.Foreground} on {pair.Background} must have at least 4.5:1 contrast."));

        Assert.All(Enum.GetValues<InteractiveVisualState>(), state =>
        {
            InteractiveStyle style = VisualSystem.For(state);
            Assert.True(
                VisualSystem.ContrastRatio(style.Foreground, style.Background) >= 4.5,
                $"{state} text must have at least 4.5:1 contrast.");
        });
    }

    [Fact]
    public void EveryMeaningfulInteractiveStateHasANonColorCue()
    {
        InteractiveVisualState[] meaningfulStates =
        [
            InteractiveVisualState.PointerHover,
            InteractiveVisualState.KeyboardFocus,
            InteractiveVisualState.Legal,
            InteractiveVisualState.Selected,
            InteractiveVisualState.Unavailable,
            InteractiveVisualState.Danger,
        ];

        Assert.All(meaningfulStates, state =>
            Assert.NotEqual(NonColorCue.None, VisualSystem.For(state).Cues));
        Assert.True(VisualSystem.For(InteractiveVisualState.KeyboardFocus).Cues
            .HasFlag(NonColorCue.FocusRing));
        Assert.True(VisualSystem.For(InteractiveVisualState.Unavailable).Cues
            .HasFlag(NonColorCue.Disabled));
        Assert.False(VisualSystem.For(InteractiveVisualState.Unavailable).Enabled);
    }

    [Fact]
    public void InteractiveStatesRemainPairwiseDistinctWithoutTheirColors()
    {
        var signatures = Enum.GetValues<InteractiveVisualState>()
            .Select(state => VisualSystem.For(state))
            .Select(style => new
            {
                style.ThemeVariation,
                style.Cues,
                style.BorderWidth,
                style.FocusRingWidth,
                style.VerticalOffset,
                style.Enabled,
            })
            .ToArray();

        Assert.Equal(signatures.Length, signatures.Distinct().Count());
    }

    [Fact]
    public void SupportedScalesPreserveReadableTypeAndPointerTargets()
    {
        Assert.Equal(
            [InterfaceScale.Standard, InterfaceScale.Large, InterfaceScale.ExtraLarge],
            VisualSystem.SupportedScales);

        Assert.All(VisualSystem.SupportedScales, scale =>
        {
            TypeMetrics type = VisualSystem.Type(scale);
            SpacingMetrics spacing = VisualSystem.Spacing(scale);
            ControlMetrics controls = VisualSystem.Controls(scale);

            Assert.True(type.Body >= 16);
            Assert.True(type.Caption >= 14);
            Assert.True(type.DisplayTitle > type.Heading);
            Assert.True(type.Heading > type.Body);
            Assert.True(type.Body > type.Caption);
            Assert.True(type.Caption > type.Eyebrow);
            Assert.True(spacing.ExtraSmall >= 4);
            Assert.True(spacing.ExtraSmall < spacing.Small);
            Assert.True(spacing.Small < spacing.Medium);
            Assert.True(spacing.Medium < spacing.Large);
            Assert.True(spacing.Large < spacing.ExtraLarge);
            Assert.True(spacing.ExtraLarge < spacing.Section);
            Assert.True(controls.MinimumHeight >= 44);
            Assert.True(controls.MinimumPointerTarget >= 44);
            Assert.True(controls.MinimumButtonWidth >= 96);
            Assert.True(controls.FocusRingWidth >= 3);
        });
    }

    [Fact]
    public void LargerScaleNeverShrinksAnyMetric()
    {
        TypeMetrics standardType = VisualSystem.Type(InterfaceScale.Standard);
        TypeMetrics largeType = VisualSystem.Type(InterfaceScale.Large);
        TypeMetrics extraLargeType = VisualSystem.Type(InterfaceScale.ExtraLarge);
        ControlMetrics standardControls = VisualSystem.Controls(InterfaceScale.Standard);
        ControlMetrics largeControls = VisualSystem.Controls(InterfaceScale.Large);
        ControlMetrics extraLargeControls = VisualSystem.Controls(InterfaceScale.ExtraLarge);

        Assert.True(standardType.DisplayTitle < largeType.DisplayTitle);
        Assert.True(largeType.DisplayTitle < extraLargeType.DisplayTitle);
        Assert.True(standardType.Body < largeType.Body);
        Assert.True(largeType.Body < extraLargeType.Body);
        Assert.True(standardControls.MinimumPointerTarget < largeControls.MinimumPointerTarget);
        Assert.True(largeControls.MinimumPointerTarget < extraLargeControls.MinimumPointerTarget);
    }

    [Fact]
    public void CardSizesHavePredictableGeometryAndCompleteDisclosure()
    {
        foreach (InterfaceScale scale in VisualSystem.SupportedScales)
        {
            CardLayoutMetrics full = VisualSystem.Card(CardDisplaySize.Full, scale);
            CardLayoutMetrics board = VisualSystem.Card(CardDisplaySize.Board, scale);

            Assert.True(full.Width > board.Width);
            Assert.True(full.MinimumHeight > board.MinimumHeight);
            Assert.True(board.ShowSubtitle);
            Assert.True(board.ShowTraits);
            Assert.True(board.ShowPrintedStats);
        }

        Assert.True(
            VisualSystem.Card(CardDisplaySize.Board, InterfaceScale.Standard).Width
            < VisualSystem.Card(CardDisplaySize.Board, InterfaceScale.Large).Width);
        Assert.True(
            VisualSystem.Card(CardDisplaySize.Board, InterfaceScale.Large).Width
            < VisualSystem.Card(CardDisplaySize.Board, InterfaceScale.ExtraLarge).Width);
    }

    [Fact]
    public void GodotVariationsAreStableAndUnique()
    {
        string[] variations =
        [
            GodotThemeVariations.DisplayTitle,
            GodotThemeVariations.BriefingTitle,
            GodotThemeVariations.Heading,
            GodotThemeVariations.Body,
            GodotThemeVariations.BodyMuted,
            GodotThemeVariations.Eyebrow,
            GodotThemeVariations.Caption,
            GodotThemeVariations.MutedText,
            GodotThemeVariations.EncounterText,
            GodotThemeVariations.DangerText,
            GodotThemeVariations.StatusText,
            GodotThemeVariations.ShellPanel,
            GodotThemeVariations.SurfacePanel,
            GodotThemeVariations.StatusPanel,
            GodotThemeVariations.DangerStatusPanel,
            GodotThemeVariations.TightStack,
            GodotThemeVariations.Stack,
            GodotThemeVariations.WideRow,
            GodotThemeVariations.CompactRow,
            GodotThemeVariations.DataGrid,
            GodotThemeVariations.MultiSelectButton,
            GodotThemeVariations.BoardArea,
            GodotThemeVariations.BoardCard,
            GodotThemeVariations.ConcealedCard,
            GodotThemeVariations.FocusedCard,
            GodotThemeVariations.CardTitle,
            GodotThemeVariations.CardRules,
            GodotThemeVariations.CardLiveValue,
            GodotThemeVariations.CardPrintedValue,
            GodotThemeVariations.CardState,
            GodotThemeVariations.PrimaryButton,
            GodotThemeVariations.ChoiceButton,
            GodotThemeVariations.LegalTargetButton,
            GodotThemeVariations.SelectedTargetButton,
            GodotThemeVariations.UnavailableButton,
        ];

        Assert.All(variations, variation => Assert.False(string.IsNullOrWhiteSpace(variation)));
        Assert.Equal(variations.Length, variations.Distinct(StringComparer.Ordinal).Count());
    }
}
