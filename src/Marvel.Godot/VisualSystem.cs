namespace Marvel.Godot;

/// <summary>An sRGB color whose channels do not depend on a UI framework.</summary>
public readonly record struct VisualColor(byte Red, byte Green, byte Blue)
{
    /// <summary>Creates a color from the conventional 24-bit RRGGBB representation.</summary>
    public static VisualColor FromRgb(uint rgb) => new(
        (byte)(rgb >> 16),
        (byte)(rgb >> 8),
        (byte)rgb);
}

/// <summary>The semantic colors shared by every client surface.</summary>
public sealed record VisualPalette(
    VisualColor Canvas,
    VisualColor Surface,
    VisualColor RaisedSurface,
    VisualColor Text,
    VisualColor MutedText,
    VisualColor Accent,
    VisualColor OnAccent,
    VisualColor Legal,
    VisualColor Selected,
    VisualColor Danger,
    VisualColor Unavailable,
    VisualColor Outline,
    VisualColor Focus);

/// <summary>Interactive states with distinct visual and non-color treatment.</summary>
public enum InteractiveVisualState
{
    Resting,
    PointerHover,
    KeyboardFocus,
    Legal,
    Selected,
    Unavailable,
    Danger,
}

/// <summary>Cues that continue to communicate state without color perception.</summary>
[Flags]
public enum NonColorCue
{
    None = 0,
    Raised = 1 << 0,
    FocusRing = 1 << 1,
    LegalMarker = 1 << 2,
    Checkmark = 1 << 3,
    Pressed = 1 << 4,
    Disabled = 1 << 5,
    WarningIcon = 1 << 6,
}

/// <summary>A semantic control treatment that a renderer can map to native theme values.</summary>
public sealed record InteractiveStyle(
    string ThemeVariation,
    VisualColor Foreground,
    VisualColor Background,
    VisualColor Border,
    NonColorCue Cues,
    int BorderWidth,
    int FocusRingWidth,
    int VerticalOffset,
    bool Enabled);

/// <summary>The discrete UI sizes supported by the desktop client.</summary>
public enum InterfaceScale
{
    Percent50 = 50,
    Percent60 = 60,
    Percent70 = 70,
    Percent80 = 80,
    Percent90 = 90,
    Percent100 = 100,
    Percent110 = 110,
    Percent120 = 120,
    Percent130 = 130,
    Percent140 = 140,
    Percent150 = 150,

    Compact = Percent80,
    Standard = Percent100,
    Large = Percent120,
    ExtraLarge = Percent150,
}

/// <summary>The two disclosure levels supported by a procedural card.</summary>
public enum CardDisplaySize
{
    Full,
    Board,
    Hand,
}

/// <summary>Deterministic card geometry and text disclosure for one display size.</summary>
public sealed record CardLayoutMetrics(
    int Width,
    int MinimumHeight,
    bool ShowSubtitle,
    bool ShowTraits,
    bool ShowPrintedStats);

/// <summary>Font sizes in logical pixels for one supported interface scale.</summary>
public sealed record TypeMetrics(
    int DisplayTitle,
    int Heading,
    int Body,
    int Caption,
    int Eyebrow);

/// <summary>Layout intervals in logical pixels for one supported interface scale.</summary>
public sealed record SpacingMetrics(
    int ExtraSmall,
    int Small,
    int Medium,
    int Large,
    int ExtraLarge,
    int Section);

/// <summary>Interactive-control dimensions in logical pixels for one supported scale.</summary>
public sealed record ControlMetrics(
    int MinimumHeight,
    int MinimumPointerTarget,
    int MinimumButtonWidth,
    int FocusRingWidth,
    int CornerRadius);

/// <summary>Desktop workbench dimensions derived from the current viewport and scale.</summary>
public sealed record DesktopPlayMetrics(
    int DecisionWidth,
    int DecisionMinimumHeight,
    int BoardAreaWidth);

/// <summary>A viewport-safe origin for a floating inspector.</summary>
public sealed record FloatingPanelPosition(int X, int Y);

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
            >= 1800 => Math.Clamp((int)Math.Ceiling(viewportWidth * 0.37), 680, 720),
            >= 1500 => 600,
            >= 1200 => Math.Clamp((int)Math.Ceiling(viewportWidth * 0.42), 500, 560),
            _ => Math.Clamp((int)Math.Ceiling(viewportWidth * 0.39), 390, 440),
        };
        CardLayoutMetrics card = Card(CardDisplaySize.Board, scale);
        SpacingMetrics spacing = Spacing(scale);
        return new DesktopPlayMetrics(
            decisionWidth,
            DecisionMinimumHeight: viewportHeight < 800
                ? Math.Max(270, Scale(220, scale))
                : Math.Max(300, Scale(320, scale)),
            BoardAreaWidth: checked(card.Width * 2));
    }

    /// <summary>Returns card geometry without shrinking type to fit content.</summary>
    public static CardLayoutMetrics Card(CardDisplaySize size, InterfaceScale scale) => size switch
    {
        CardDisplaySize.Full => new(
            Scale(320, scale), Scale(520, scale),
            ShowSubtitle: true, ShowTraits: true, ShowPrintedStats: true),
        CardDisplaySize.Board => new(
            Scale(210, scale), Scale(190, scale),
            ShowSubtitle: false, ShowTraits: false, ShowPrintedStats: true),
        CardDisplaySize.Hand => new(
            Scale(210, scale), Scale(52, scale),
            ShowSubtitle: false, ShowTraits: false, ShowPrintedStats: false),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "unsupported card size"),
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

/// <summary>Godot theme variation names shared by scene and procedural controls.</summary>
public static class GodotThemeVariations
{
    public const string DisplayTitle = nameof(DisplayTitle);
    public const string BriefingTitle = nameof(BriefingTitle);
    public const string Heading = nameof(Heading);
    public const string Body = nameof(Body);
    public const string BodyMuted = nameof(BodyMuted);
    public const string Eyebrow = nameof(Eyebrow);
    public const string Caption = nameof(Caption);
    public const string MutedText = nameof(MutedText);
    public const string EncounterText = nameof(EncounterText);
    public const string DangerText = nameof(DangerText);
    public const string StatusText = nameof(StatusText);
    public const string ShellPanel = nameof(ShellPanel);
    public const string SurfacePanel = nameof(SurfacePanel);
    public const string StatusPanel = nameof(StatusPanel);
    public const string DangerStatusPanel = nameof(DangerStatusPanel);
    public const string TightStack = nameof(TightStack);
    public const string Stack = nameof(Stack);
    public const string WideRow = nameof(WideRow);
    public const string CompactRow = nameof(CompactRow);
    public const string DataGrid = nameof(DataGrid);
    public const string MultiSelectButton = nameof(MultiSelectButton);
    public const string BoardArea = nameof(BoardArea);
    public const string BoardCard = nameof(BoardCard);
    public const string ConcealedCard = nameof(ConcealedCard);
    public const string FocusedCard = nameof(FocusedCard);
    public const string CardTitle = nameof(CardTitle);
    public const string CardRules = nameof(CardRules);
    public const string CardLiveValue = nameof(CardLiveValue);
    public const string CardPrintedValue = nameof(CardPrintedValue);
    public const string CardState = nameof(CardState);
    public const string PrimaryButton = nameof(PrimaryButton);
    public const string ChoiceButton = nameof(ChoiceButton);
    public const string LegalTargetButton = nameof(LegalTargetButton);
    public const string SelectedTargetButton = nameof(SelectedTargetButton);
    public const string UnavailableButton = nameof(UnavailableButton);
}
