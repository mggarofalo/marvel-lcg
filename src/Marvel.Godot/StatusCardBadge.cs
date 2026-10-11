using Godot;

namespace Marvel.Godot;

/// <summary>Shape and lettering distinguish supplied status cards from defense actions.</summary>
internal sealed partial class StatusCardBadge : Control
{
    private string status = "";

    internal static bool Supports(string name) => name.ToUpperInvariant() is "TOUGH" or "STUNNED" or "CONFUSED";

    internal static Control Create(string name, string description, bool compact)
    {
        var row = new HBoxContainer { Name = "StatusCard" + name, MouseFilter = MouseFilterEnum.Ignore,
            AccessibilityName = description, TooltipText = description };
        row.AddChild(new StatusCardBadge { status = name.ToUpperInvariant(),
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
        Vector2[] shape = Enumerable.Range(0, status == "STUNNED" ? 16 : 8).Select(index =>
        {
            int count = status == "STUNNED" ? 16 : 8;
            float radius = status == "STUNNED" && index % 2 != 0 ? 9 : 13;
            return center + Vector2.FromAngle((float)(index * Math.Tau / count - Math.PI / 8)) * radius;
        }).ToArray();
        if (status == "CONFUSED") DrawCircle(center, 12, color, filled: false, width: 3, antialiased: true);
        else { DrawColoredPolygon(shape, color); CardEdgeSmoothing.Draw(this, shape, color); }
        DrawString(CardTypography.Bold, new Vector2(9, 20), status[..1], fontSize: 17,
            modulate: status == "CONFUSED" ? color : CardFaceStyle.Ink);
    }
}
