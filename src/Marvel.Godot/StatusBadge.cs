using Godot;

namespace Marvel.Godot;

/// <summary>Building block: a status card on a card or seat, told apart by shape and letter, never by color alone.</summary>
/// <remarks>
/// Tough is an octagon, stunned a sixteen-point star and confused a ring; each carries its initial.
/// The shapes, letters and palette tints are our choices; which statuses a card has, and how many,
/// is the engine's answer. The badge is two body lines tall at the reading scale.
/// </remarks>
internal sealed partial class StatusBadge : Control
{
    private string status = "";
    private int letter;

    /// <summary>Whether a status has a badge; other statuses read as a <see cref="Chip"/>.</summary>
    internal static bool Supports(string name) => name.ToUpperInvariant() is "TOUGH" or "STUNNED" or "CONFUSED";

    /// <summary>A status's outline: its point count, and whether it is drawn as a ring rather than filled.</summary>
    internal static (int Points, bool Ring) Outline(string name) => name.ToUpperInvariant() switch
    {
        "STUNNED" => (16, false),
        "CONFUSED" => (0, true),
        "TOUGH" => (8, false),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "no badge for this status"),
    };

    /// <summary>A status's palette tint.</summary>
    internal static VisualColor Tint(string name) => name.ToUpperInvariant() switch
    {
        "TOUGH" => VisualSystem.Palette.Accent,
        "STUNNED" => VisualSystem.Palette.Danger,
        "CONFUSED" => VisualSystem.Palette.Legal,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "no badge for this status"),
    };

    /// <summary>The words beside a badge: the count when compact and above one, else the description.</summary>
    internal static string Caption(string description, int count, bool compact) =>
        compact ? count > 1 ? $"×{count}" : "" : description;

    /// <summary>The badge's side in pixels: two body lines.</summary>
    internal static int Side(InterfaceScale scale) => 2 * VisualSystem.Type(scale).Body;

    /// <summary>The badge and its caption; the description is always the tooltip.</summary>
    internal static Control Create(string name, string description, int count, bool compact, InterfaceScale scale)
    {
        var row = new HBoxContainer { Name = "StatusCard" + name, MouseFilter = MouseFilterEnum.Pass,
            AccessibilityName = description, TooltipText = description };
        row.AddThemeConstantOverride("separation", VisualSystem.Spacing(scale).ExtraSmall);
        int side = Side(scale);
        row.AddChild(new StatusBadge { status = name.ToUpperInvariant(), letter = VisualSystem.Type(scale).Heading,
            CustomMinimumSize = new Vector2(side, side), MouseFilter = MouseFilterEnum.Ignore });
        Label label = TableBlockStyle.Whole("Caption", Caption(description, count, compact), scale, VisualSystem.Palette.Text);
        label.AddThemeFontOverride("font", CardTypography.Bold);
        label.MouseFilter = MouseFilterEnum.Ignore;
        label.Visible = label.Text.Length > 0;
        row.AddChild(label);
        return row;
    }

    public override void _Draw()
    {
        float side = Math.Min(Size.X, Size.Y);
        Vector2 center = new(side / 2, side / 2);
        Color color = ClientTheme.ToGodot(Tint(status));
        (int points, bool ring) = Outline(status);
        float outer = side * 0.46f;
        if (ring) DrawCircle(center, outer * 0.92f, color, filled: false, width: Math.Max(2, side / 9), antialiased: true);
        else
        {
            Vector2[] shape = [.. Enumerable.Range(0, points).Select(index =>
            {
                float radius = points == 16 && index % 2 != 0 ? outer * 0.7f : outer;
                return center + Vector2.FromAngle((float)(index * Math.Tau / points - Math.PI / 8)) * radius;
            })];
            DrawColoredPolygon(shape, color);
            CardEdgeSmoothing.Draw(this, shape, color);
        }
        string initial = status[..1];
        Vector2 extent = CardTypography.Bold.GetStringSize(initial, fontSize: letter);
        DrawString(CardTypography.Bold, center + new Vector2(-extent.X / 2, letter * 0.36f), initial, fontSize: letter,
            modulate: ring ? color : ClientTheme.ToGodot(VisualSystem.Palette.OnAccent));
    }
}
