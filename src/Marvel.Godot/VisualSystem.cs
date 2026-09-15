namespace Marvel.Godot;

/// <summary>
/// Framework-independent visual tokens for the Godot client.
///
/// These values are a product choice. The tabletop rules do not define client
/// colors, type, spacing, accessibility cues or control dimensions.
/// </summary>
public static class VisualSystem
{
    private static readonly InterfaceScale[] Scales =
    [
        InterfaceScale.Percent50,
        InterfaceScale.Percent60,
        InterfaceScale.Percent70,
        InterfaceScale.Percent80,
        InterfaceScale.Percent90,
        InterfaceScale.Percent100,
        InterfaceScale.Percent110,
        InterfaceScale.Percent120,
        InterfaceScale.Percent130,
        InterfaceScale.Percent140,
        InterfaceScale.Percent150,
    ];

    public static VisualPalette Palette { get; } = new(
        Canvas: VisualColor.FromRgb(0x092A2C),
        Surface: VisualColor.FromRgb(0x101C26),
        RaisedSurface: VisualColor.FromRgb(0x172B34),
        Text: VisualColor.FromRgb(0xF2EDD9),
        MutedText: VisualColor.FromRgb(0xB2C0C2),
        Accent: VisualColor.FromRgb(0xF2C14E),
        OnAccent: VisualColor.FromRgb(0x071019),
        Legal: VisualColor.FromRgb(0x4C9ED9),
        Selected: VisualColor.FromRgb(0xF2C14E),
        Danger: VisualColor.FromRgb(0xE05B5E),
        Unavailable: VisualColor.FromRgb(0x9AA6B8),
        Outline: VisualColor.FromRgb(0x78959D),
        Focus: VisualColor.FromRgb(0xF2C14E));

    /// <summary>Every scale the client promises to render and test.</summary>
    public static IReadOnlyList<InterfaceScale> SupportedScales => Scales;

    /// <summary>The user-facing percentage for a discrete interface scale.</summary>
    public static double ScalePercent(InterfaceScale scale)
    {
        _ = ScaleFactor(scale);
        return (int)scale;
    }

    /// <summary>
    /// Places a floating panel on the roomier horizontal side of the pointer
    /// and clamps it to the visible desktop.
    /// </summary>
    public static FloatingPanelPosition PlaceFloatingPanel(
        int viewportWidth,
        int viewportHeight,
        int pointerX,
        int pointerY,
        int panelWidth,
        int panelHeight,
        int margin = 12,
        int pointerGap = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(viewportWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(viewportHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(panelWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(panelHeight);

        int maximumX = Math.Max(margin, viewportWidth - panelWidth - margin);
        int maximumY = Math.Max(margin, viewportHeight - panelHeight - margin);
        int roomOnRight = viewportWidth - pointerX - margin;
        int roomOnLeft = pointerX - margin;
        int desiredX = roomOnRight >= roomOnLeft
            ? pointerX + pointerGap
            : pointerX - pointerGap - panelWidth;
        int desiredY = pointerY - panelHeight / 2;
        return new FloatingPanelPosition(
            Math.Clamp(desiredX, margin, maximumX),
            Math.Clamp(desiredY, margin, maximumY));
    }

    /// <summary>Returns the semantic treatment for one interactive state.</summary>
    public static InteractiveStyle For(InteractiveVisualState state) => state switch
    {
        InteractiveVisualState.Resting => new(
            GodotThemeVariations.ChoiceButton,
            Palette.Text,
            Palette.Surface,
            Palette.Outline,
            NonColorCue.None,
            BorderWidth: 1,
            FocusRingWidth: 0,
            VerticalOffset: 0,
            Enabled: true),
        InteractiveVisualState.PointerHover => new(
            GodotThemeVariations.ChoiceButton,
            Palette.Text,
            Palette.RaisedSurface,
            Palette.Accent,
            NonColorCue.Raised,
            BorderWidth: 2,
            FocusRingWidth: 0,
            VerticalOffset: -2,
            Enabled: true),
        InteractiveVisualState.KeyboardFocus => new(
            GodotThemeVariations.ChoiceButton,
            Palette.Text,
            Palette.Surface,
            Palette.Focus,
            NonColorCue.FocusRing,
            BorderWidth: 2,
            FocusRingWidth: 3,
            VerticalOffset: 0,
            Enabled: true),
        InteractiveVisualState.Legal => new(
            GodotThemeVariations.LegalTargetButton,
            Palette.Text,
            Palette.Surface,
            Palette.Legal,
            NonColorCue.LegalMarker,
            BorderWidth: 2,
            FocusRingWidth: 0,
            VerticalOffset: 0,
            Enabled: true),
        InteractiveVisualState.Selected => new(
            GodotThemeVariations.SelectedTargetButton,
            Palette.OnAccent,
            Palette.Selected,
            Palette.Selected,
            NonColorCue.Checkmark | NonColorCue.Pressed,
            BorderWidth: 3,
            FocusRingWidth: 0,
            VerticalOffset: 1,
            Enabled: true),
        InteractiveVisualState.Unavailable => new(
            GodotThemeVariations.UnavailableButton,
            Palette.Unavailable,
            Palette.Surface,
            Palette.Outline,
            NonColorCue.Disabled,
            BorderWidth: 1,
            FocusRingWidth: 0,
            VerticalOffset: 0,
            Enabled: false),
        InteractiveVisualState.Danger => new(
            GodotThemeVariations.PrimaryButton,
            Palette.OnAccent,
            Palette.Danger,
            Palette.Danger,
            NonColorCue.WarningIcon,
            BorderWidth: 3,
            FocusRingWidth: 0,
            VerticalOffset: 0,
            Enabled: true),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "unknown visual state"),
    };

    public static TypeMetrics Type(InterfaceScale scale) => new(
        Scale(36, scale),
        Scale(18, scale),
        Scale(15, scale),
        Scale(13, scale),
        Scale(10, scale));

    public static SpacingMetrics Spacing(InterfaceScale scale) => new(
        Scale(4, scale),
        Scale(8, scale),
        Scale(12, scale),
        Scale(16, scale),
        Scale(24, scale),
        Scale(32, scale));

    public static ControlMetrics Controls(InterfaceScale scale) => new(
        MinimumHeight: Scale(44, scale),
        MinimumPointerTarget: Scale(44, scale),
        MinimumButtonWidth: Scale(96, scale),
        FocusRingWidth: Scale(3, scale),
        CornerRadius: Scale(8, scale));

    /// <summary>Keeps the active decision dominant while preserving a usable table.</summary>
    public static DesktopPlayMetrics DesktopPlay(
        int viewportWidth,
        int viewportHeight,
        InterfaceScale scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(viewportWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(viewportHeight);

        int decisionWidth = viewportWidth switch
        {
            >= 1800 => Math.Clamp((int)Math.Ceiling(viewportWidth * 0.26), 480, 520),
            >= 1500 => 600,
            >= 1200 => Math.Clamp((int)Math.Ceiling(viewportWidth * 0.36), 450, 500),
            _ => Math.Clamp((int)Math.Ceiling(viewportWidth * 0.39), 390, 440),
        };
        CardLayoutMetrics card = Card(CardDisplaySize.Board, scale);
        return new DesktopPlayMetrics(
            decisionWidth,
            DecisionMinimumHeight: viewportHeight < 800
                ? Math.Max(270, Scale(220, scale))
                : Math.Max(300, Scale(320, scale)),
            BoardAreaWidth: checked(card.Width + 32));
    }

    /// <summary>Returns card geometry without shrinking type to fit content.</summary>
    public static CardLayoutMetrics Card(CardDisplaySize size, InterfaceScale scale) => size switch
    {
        CardDisplaySize.Full => new(
            Scale(400, scale), Scale(560, scale),
            ShowSubtitle: true, ShowTraits: true, ShowPrintedStats: true),
        CardDisplaySize.Board => new(
            Scale(176, scale), Scale(208, scale),
            ShowSubtitle: false, ShowTraits: false, ShowPrintedStats: true),
        CardDisplaySize.Hand => new(
            Scale(156, scale), Scale(184, scale),
            ShowSubtitle: false, ShowTraits: false, ShowPrintedStats: false),
        // The opening hand is the one supported six-card decision. Its scale is
        // deliberately capped so 150% preserves six independently reachable
        // choices on the 1920px desktop table; this is a product layout choice.
        CardDisplaySize.Mulligan => new(
            MulliganWidth(scale),
            Scale(116, scale),
            ShowSubtitle: false, ShowTraits: false, ShowPrintedStats: false),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "unsupported card size"),
    };

    /// <summary>Normalizes unequal Unicode resource glyphs inside one square rhythm.</summary>
    public static ResourceIconMetrics ResourceIcon(string glyph, InterfaceScale scale) => glyph switch
    {
        "P" or "M" or "E" or "W" => new(Scale(26, scale), Scale(22, scale)),
        _ => throw new ArgumentOutOfRangeException(nameof(glyph), glyph, "unknown resource glyph"),
    };

    /// <summary>Maps a visible card kind to its presentation-only frame family.</summary>
    public static CardFrameProfile CardFrame(string kind) => kind switch
    {
        "ALTER EGO" or "HERO" => new(
            CardFrameFamily.Identity, GodotThemeVariations.IdentityCard, "Identity"),
        "ALLY" or "EVENT" or "RESOURCE" or "SUPPORT" or "UPGRADE"
            or "PLAYER SIDE SCHEME" => new(
                CardFrameFamily.Player, GodotThemeVariations.PlayerCard, "Player card"),
        "ENCOUNTER VILLAIN" or "LEADER" or "MINION" => new(
            CardFrameFamily.Enemy, GodotThemeVariations.EnemyCard, "Enemy"),
        "MAIN SCHEME" or "ENCOUNTER SIDE SCHEME" => new(
            CardFrameFamily.Scheme, GodotThemeVariations.SchemeCard, "Scheme"),
        _ => new(
            CardFrameFamily.Environment, GodotThemeVariations.EnvironmentCard, "Encounter card"),
    };

    /// <summary>Computes the WCAG contrast ratio for two sRGB colors.</summary>
    public static double ContrastRatio(VisualColor first, VisualColor second)
    {
        double lighter = Math.Max(Luminance(first), Luminance(second));
        double darker = Math.Min(Luminance(first), Luminance(second));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static int Scale(int value, InterfaceScale scale) =>
        (int)Math.Ceiling(value * ScaleFactor(scale));

    private static int MulliganWidth(InterfaceScale scale) =>
        (int)scale <= 100 ? Scale(168, scale) : 168;

    private static double ScaleFactor(InterfaceScale scale)
    {
        int percentage = (int)scale;
        if (percentage is < 50 or > 150 || percentage % 10 != 0)
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "unsupported interface scale");
        return percentage / 100.0;
    }

    private static double Luminance(VisualColor color) =>
        0.2126 * Linear(color.Red)
        + 0.7152 * Linear(color.Green)
        + 0.0722 * Linear(color.Blue);

    private static double Linear(byte channel)
    {
        double value = channel / 255.0;
        return value <= 0.04045
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
