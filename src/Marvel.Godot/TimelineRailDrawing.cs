using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Draws only visible ordinal bins, independent of total history length.</summary>
internal static class TimelineRailDrawing
{
    internal static void Paint(Control rail, TimelineViewport viewport, IReadOnlyList<TimelineOccurrence> entries)
    {
        Color muted = ClientTheme.ToGodot(VisualSystem.Palette.MutedText);
        Color accent = ClientTheme.ToGodot(VisualSystem.Palette.Accent);
        float y = rail.Size.Y / 2;
        rail.DrawLine(new Vector2(0, y), new Vector2(rail.Size.X, y), muted, 1, true);
        int painted = 0;
        foreach (var bin in viewport.Bins(rail.Size.X))
        {
            float x = (float)bin.Fraction * rail.Size.X;
            bool selected = viewport.Selected >= bin.First && viewport.Selected <= bin.Last;
            Color ink = selected ? accent : CardFaceStyle.Paper;
            float radius = bin.First == bin.Last ? 3 : 6;
            rail.DrawCircle(new Vector2(x, y), radius, ink, antialiased: true);
            if (selected) rail.DrawArc(new Vector2(x, y), radius + 3, 0, Mathf.Tau, 24, accent, 1, true);
            DrawKind(rail, entries[bin.First].Event, new Vector2(x, y), ink, bin.First == bin.Last);
            painted++;
        }
        if (rail.HasFocus()) rail.DrawRect(new Rect2(Vector2.One, rail.Size - Vector2.One * 2), accent, false, 1);
        rail.SetMeta("painted_bins", painted);
    }

    private static void DrawKind(Control rail, EventPresentation entry, Vector2 center, Color ink, bool detailed)
    {
        if (entry.Activity is { PhaseBoundary: true } phase)
        {
            rail.DrawLine(center + new Vector2(0, -10), center + new Vector2(0, 10), ink, 2, true);
            if (detailed) rail.DrawString(CardTypography.Body, center + new Vector2(5, 4),
                $"{phase.Round}{phase.Phase[..1]}", fontSize: 10, modulate: ink);
            return;
        }
        if (entry.Motion is EventMotionKind.Attack or EventMotionKind.Damage)
        {
            rail.DrawLine(center + new Vector2(-4, -4), center + new Vector2(4, 4), ink, 2, true);
            rail.DrawLine(center + new Vector2(-4, 4), center + new Vector2(4, -4), ink, 2, true);
        }
        else if (entry.Motion == EventMotionKind.HandGain)
            rail.DrawRect(new Rect2(center - new Vector2(5, 7), new Vector2(10, 14)), ink, false, 1);
    }
}
