using Godot;

namespace Marvel.Godot;

/// <summary>Building block: a status card on a card or seat, told apart by shape and letter, never by color alone.</summary>
/// <remarks>
/// Tough is an octagon, stunned a sixteen-point star and confused a ring; each carries its initial.
/// The shapes, letters and tints are our choices; which statuses a card has is the engine's answer.
/// </remarks>
internal sealed partial class StatusBadge : Control
{
    private string status = "";

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

    /// <summary>The badge and, unless compact, its description; the description is always the tooltip.</summary>
    internal static Control Create(string name, string description, bool compact)
    {
        var row = new HBoxContainer { Name = "StatusCard" + name, MouseFilter = MouseFilterEnum.Ignore,
            AccessibilityName = description, TooltipText = description };
        row.AddChild(new StatusBadge { status = name.ToUpperInvariant(),
            CustomMinimumSize = new Vector2(28, 28), MouseFilter = MouseFilterEnum.Ignore });
        string text = compact ? (description.Contains('×') ? description.Split('×')[0].Trim() : "") : description;
        var label = new Label { Text = text, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", CardTypography.Bold);
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", Colors.White);
        row.AddChild(label);
        return row;
    }

    public override void _Draw()
    {
        Vector2 center = new(14, 14);
        Color color = status switch { "TOUGH" => new Color("e8c877"), "STUNNED" => new Color("ee947e"), _ => new Color("aab4ee") };
        (int points, bool ring) = Outline(status);
        if (ring) DrawCircle(center, 12, color, filled: false, width: 3, antialiased: true);
        else
        {
            Vector2[] shape = [.. Enumerable.Range(0, points).Select(index =>
            {
                float radius = points == 16 && index % 2 != 0 ? 9 : 13;
                return center + Vector2.FromAngle((float)(index * Math.Tau / points - Math.PI / 8)) * radius;
            })];
            DrawColoredPolygon(shape, color);
            CardEdgeSmoothing.Draw(this, shape, color);
        }
        DrawString(CardTypography.Bold, new Vector2(9, 20), status[..1], fontSize: 17,
            modulate: ring ? color : CardFaceStyle.Ink);
    }
}
