using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Draws passive causal impulses and labelled result badges at visible authorized objects.</summary>
internal sealed partial class EventActivityPulse : Control
{
    private IReadOnlyList<(EventPresentation Entry, ActivitySubject Subject, Rect2 Bounds)> subjects = [];
    private float progress;
    private bool animated;

    internal void Present(IReadOnlyList<(EventPresentation, ActivitySubject, Rect2)> references,
        double seconds, bool motion)
    {
        subjects = references;
        progress = (float)(seconds % 1);
        animated = motion;
        AccessibilityName = string.Join(" · ", references.Select(reference =>
            reference.Item2.Name + ": " + (reference.Item1.Motion == EventMotionKind.Attack
                ? ActivityVisualMeaning.Role(reference.Item1, reference.Item2)
                : ActivityVisualMeaning.LocalResult(reference.Item1, reference.Item2))));
        SetMeta("visible_result_count", references.Count);
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var group in subjects.GroupBy(subject => subject.Entry))
        {
            var source = group.FirstOrDefault(subject => subject.Subject.Role is ActivitySubjectRole.Source or ActivitySubjectRole.Boost);
            var target = group.FirstOrDefault(subject => subject.Subject.Role == ActivitySubjectRole.Target);
            if (source.Subject is not null && target.Subject is not null && source.Bounds != target.Bounds)
                DrawImpulse(source.Bounds.GetCenter(), target.Bounds.GetCenter());
        }
        var rows = new Dictionary<int, int>();
        foreach (var reference in subjects)
        {
            Rect2 rect = reference.Bounds;
            string role = ActivityVisualMeaning.Role(reference.Entry, reference.Subject);
            bool roleOnly = reference.Entry.Motion == EventMotionKind.Attack
                || reference.Subject.Role is ActivitySubjectRole.Source or ActivitySubjectRole.Defender;
            string result = roleOnly ? role : ActivityVisualMeaning.LocalResult(reference.Entry, reference.Subject);
            Color ink = ClientTheme.ToGodot(VisualSystem.Palette.Accent);
            // A bracket names a result reference; it does not replace live counters or readiness.
            Vector2 corner = rect.Position;
            DrawLine(corner, corner + new Vector2(Math.Min(38, rect.Size.X), 0), ink, 3, true);
            DrawLine(corner, corner + new Vector2(0, 22), ink, 3, true);
            // Compact sources already expose their updated counters and state inline.
            // Their transient emphasis stays inside the tile, clear of the chronology.
            if (rect.Size.Y < 110 || reference.Entry.Motion == EventMotionKind.State) continue;
            int row = rows.GetValueOrDefault(reference.Subject.Card);
            rows[reference.Subject.Card] = row + 1;
            DrawBadge(reference, result, roleOnly, row);
        }
    }

    private void DrawBadge((EventPresentation Entry, ActivitySubject Subject, Rect2 Bounds) reference,
        string result, bool roleOnly, int row)
    {
        Rect2 rect = reference.Bounds;
        Font font = CardTypography.Bold;
        float width = font.GetStringSize(result, fontSize: 16).X + 44;
        bool fits = width <= Math.Min(300, rect.Size.X);
        if (!fits) width = 30;
        Rect2 badge = new(rect.Position.X, rect.Position.Y + rect.Size.Y / 2 - 15 + row * 32, width, 30);
        DrawRect(badge, CardFaceStyle.Ink);
        Rect2 glyph = new(badge.Position + new Vector2(2, 1), new Vector2(28, 28));
        if (roleOnly) DrawRoleGlyph(reference.Subject.Role, glyph);
        else ActivityGlyphDrawing.Draw(this, reference.Entry, glyph);
        if (fits) DrawString(font, badge.Position + new Vector2(34, 21), result,
            width: width - 40, fontSize: 16, modulate: CardFaceStyle.Paper);
    }

    private void DrawRoleGlyph(ActivitySubjectRole role, Rect2 bounds)
    {
        Vector2 center = bounds.GetCenter();
        if (role == ActivitySubjectRole.Defender)
        {
            Vector2[] shield = [center + new Vector2(-10, -9), center + new Vector2(10, -9),
                center + new Vector2(8, 5), center + new Vector2(0, 12), center + new Vector2(-8, 5)];
            DrawColoredPolygon(shield, CardFaceStyle.Paper);
            CardEdgeSmoothing.Draw(this, shield, CardFaceStyle.Paper);
        }
        else if (role == ActivitySubjectRole.Target)
            DrawCircle(center, 10, CardFaceStyle.Paper, filled: false, width: 3, antialiased: true);
        else
        {
            DrawLine(center - new Vector2(10, 0), center + new Vector2(10, 0), CardFaceStyle.Paper, 3, true);
            DrawPolyline([center + new Vector2(3, -6), center + new Vector2(10, 0), center + new Vector2(3, 6)],
                CardFaceStyle.Paper, 3, true);
        }
    }

    private void DrawImpulse(Vector2 source, Vector2 target)
    {
        Color ink = ClientTheme.ToGodot(VisualSystem.Palette.Accent);
        Vector2 direction = (target - source).Normalized();
        Vector2 tip = target - direction * 18;
        DrawLine(source, tip, ink, 2, true);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawLine(tip, tip - direction * 12 + normal * 7, ink, 3, true);
        DrawLine(tip, tip - direction * 12 - normal * 7, ink, 3, true);
        if (animated) DrawCircle(source.Lerp(tip, progress), 5, CardFaceStyle.Paper, antialiased: true);
    }
}
