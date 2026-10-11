using Godot;

namespace Marvel.Godot;

/// <summary>A hollow shield interaction, distinct from a filled Tough status badge.</summary>
internal static class DefenderActionSymbol
{
    internal static void Attach(Button button, bool required)
    {
        button.AccessibilityName = required ? "Defense required — choose this defender" : "Choose this defender";
        button.Draw += () =>
        {
            Vector2 center = button.Size / 2;
            Color ink = required ? ClientTheme.ToGodot(VisualSystem.Palette.Danger) : Colors.White;
            Vector2[] shield = [center + new Vector2(-10, -12), center + new Vector2(10, -12),
                center + new Vector2(10, 1), center + new Vector2(0, 12),
                center + new Vector2(-10, 1), center + new Vector2(-10, -12)];
            button.DrawPolyline(shield, ink, 2, true);
            if (required)
            {
                button.DrawLine(center + new Vector2(0, -7), center, ink, 2, true);
                button.DrawCircle(center + new Vector2(0, 5), 1.5f, ink, true, -1, true);
                button.DrawRect(new Rect2(Vector2.One, button.Size - Vector2.One * 2), ink, false, 2);
            }
        };
    }
}
