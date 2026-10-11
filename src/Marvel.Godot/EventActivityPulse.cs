using Godot;

namespace Marvel.Godot;

/// <summary>Marks public visible subjects without moving cards or intercepting input.</summary>
internal sealed partial class EventActivityPulse : Control
{
    private IReadOnlyList<Rect2> subjects = [];
    private float intensity;
    private int ordinal;

    internal void Present(IReadOnlyList<Rect2> rectangles, int step, double seconds)
    {
        subjects = rectangles;
        ordinal = step + 1;
        intensity = 0.55f + 0.4f * (float)((Math.Sin(seconds * 6) + 1) / 2);
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (Rect2 rect in subjects)
        {
            Color ink = ClientTheme.ToGodot(VisualSystem.Palette.Accent);
            ink.A = intensity;
            CardInteractionDrawing.StrokeRect(this, rect.Grow(3), ink, 3);
            Vector2 badge = rect.Position + new Vector2(5, 5);
            DrawCircle(badge, 10, CardFaceStyle.Ink, antialiased: true);
            DrawString(ThemeDB.FallbackFont, badge + new Vector2(-4, 4), ordinal.ToString(
                System.Globalization.CultureInfo.InvariantCulture), fontSize: 12, modulate: CardFaceStyle.Paper);
        }
    }
}
