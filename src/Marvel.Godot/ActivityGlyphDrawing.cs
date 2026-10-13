using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Shape-based activity marks use the card-face palette and never require color recognition.</summary>
internal static class ActivityGlyphDrawing
{
    internal static void Draw(Control canvas, EventPresentation entry, Rect2 bounds)
    {
        Color ink = CardFaceStyle.Paper;
        Vector2 center = bounds.GetCenter();
        float radius = Math.Min(bounds.Size.X, bounds.Size.Y) * 0.32f;
        if (entry.Activity is { PhaseBoundary: true } phase)
        {
            canvas.DrawCircle(center, radius, ink, filled: false, width: 2, antialiased: true);
            canvas.DrawString(CardTypography.Bold, center + new Vector2(-5, 5),
                phase.Phase.StartsWith("Player", StringComparison.Ordinal) ? "P" : "V", fontSize: 14, modulate: ink);
            return;
        }
        switch (entry.Motion)
        {
            case EventMotionKind.Heal:
                canvas.DrawRect(new Rect2(center.X - radius, center.Y - 3, radius * 2, 6), ink);
                canvas.DrawRect(new Rect2(center.X - 3, center.Y - radius, 6, radius * 2), ink);
                break;
            case EventMotionKind.Threat:
                Vector2[] diamond = [center + new Vector2(0, -radius), center + new Vector2(radius, 0),
                    center + new Vector2(0, radius), center + new Vector2(-radius, 0)];
                canvas.DrawColoredPolygon(diamond, ink);
                CardEdgeSmoothing.Draw(canvas, diamond, ink);
                break;
            case EventMotionKind.Damage:
            case EventMotionKind.Defeat:
                DrawDamage(canvas, center, radius, ink);
                break;
            case EventMotionKind.Attack:
                canvas.DrawLine(center - new Vector2(radius, 0), center + new Vector2(radius, 0), ink, 4, true);
                canvas.DrawPolyline([center + new Vector2(2, -radius * 0.65f), center + new Vector2(radius, 0),
                    center + new Vector2(2, radius * 0.65f)], ink, 4, true);
                break;
            case EventMotionKind.Counter:
                canvas.DrawCircle(center, radius, ink, antialiased: true);
                canvas.DrawCircle(center, radius * 0.6f, CardFaceStyle.Ink, antialiased: true);
                break;
            case EventMotionKind.Status:
                DrawStatus(canvas, entry, center, radius, ink);
                break;
            default:
                DrawBoostOrReference(canvas, entry, bounds, center, radius, ink);
                break;
        }
    }

    private static void DrawDamage(Control canvas, Vector2 center, float radius, Color ink)
    {
        Vector2[] burst = Enumerable.Range(0, 12).Select(index => center + Vector2.FromAngle(index * Mathf.Tau / 12)
            * (index % 2 == 0 ? radius : radius * 0.48f)).ToArray();
        canvas.DrawColoredPolygon(burst, ink);
        CardEdgeSmoothing.Draw(canvas, burst, ink);
        canvas.DrawLine(center + new Vector2(-5, 0), center + new Vector2(5, 0), CardFaceStyle.Ink, 3);
    }

    private static void DrawStatus(Control canvas, EventPresentation entry, Vector2 center, float radius, Color ink)
    {
        canvas.DrawCircle(center, radius, ink, filled: false, width: 2, antialiased: true);
        canvas.DrawLine(center - new Vector2(radius * 0.6f, 0), center + new Vector2(radius * 0.6f, 0), ink, 3, true);
        if (entry.Activity?.StatusChange == ActivityStatusChange.Applied)
            canvas.DrawLine(center - new Vector2(0, radius * 0.6f), center + new Vector2(0, radius * 0.6f), ink, 3, true);
    }

    private static void DrawBoostOrReference(Control canvas, EventPresentation entry, Rect2 bounds, Vector2 center, float radius, Color ink)
    {
        if (entry.Motion == EventMotionKind.Flip)
        {
            canvas.DrawArc(center, radius, -.7f, 2.4f, 20, ink, 2, true);
            canvas.DrawArc(center, radius, 2.5f, 5.6f, 20, ink, 2, true);
            canvas.DrawLine(center + new Vector2(radius, -4), center + new Vector2(radius - 7, -4), ink, 2, true);
            canvas.DrawLine(center + new Vector2(-radius, 4), center + new Vector2(-radius + 7, 4), ink, 2, true);
        }
        else if (entry.Activity?.Quantities.Any(quantity => quantity.Field == "boost_icons") == true)
            canvas.DrawPolyline([center + new Vector2(-radius, radius * 0.5f), center + new Vector2(0, -radius * 0.5f),
                center + new Vector2(radius, radius * 0.5f)], ink, 5, true);
        else if (entry.Motion == EventMotionKind.State)
        {
            canvas.DrawCircle(center, radius, ink, filled: false, width: 2, antialiased: true);
            canvas.DrawCircle(center, 3, ink, antialiased: true);
        }
        else DrawReference(canvas, bounds, ink);
    }

    internal static void DrawReference(Control canvas, Rect2 bounds, Color ink, bool landscape = false)
    {
        float height = Math.Min(bounds.Size.Y - 4, 38);
        Rect2 card = new(bounds.GetCenter() - new Vector2(height / 3, height / 2), new Vector2(height * 2 / 3, height));
        if (landscape) card = new Rect2(bounds.GetCenter() - new Vector2(height / 2, height / 3), new Vector2(height, height * 2 / 3));
        canvas.DrawRect(card, CardFaceStyle.Ink);
        CardInteractionDrawing.StrokeRect(canvas, card, ink, 2);
        // An occurrence reference uses a physical 2:3 silhouette, with no reconstructed face.
        canvas.DrawLine(card.Position + new Vector2(4, 7), card.Position + new Vector2(card.Size.X - 4, 7), ink, 2);
        canvas.DrawLine(card.Position + new Vector2(4, card.Size.Y - 7), card.End - new Vector2(4, 7), ink, 2);
    }
}
