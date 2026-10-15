using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Draws passive causal impulses and labelled result badges at visible authorized objects.</summary>
internal sealed partial class EventActivityPulse : Control
{
    private IReadOnlyList<(EventPresentation Entry, ActivitySubject Subject, Rect2 Bounds)> subjects = [];
    private float progress;
    private bool animated;
    private bool impacted;
    private float emphasis;

    internal void Present(IReadOnlyList<(EventPresentation, ActivitySubject, Rect2)> references,
        double seconds, bool motion, double onset = 0)
    {
        subjects = references;
        progress = Mathf.Clamp((float)(seconds / .22), 0, 1);
        animated = motion;
        impacted = !motion || seconds >= onset;
        emphasis = !motion ? .75f : !impacted ? .45f
            : Mathf.Lerp(1, .65f, Mathf.Clamp((float)((seconds - onset) / .2), 0, 1));
        AccessibilityName = string.Join(" · ", references.Select(reference =>
            reference.Item2.Name + ": " + (reference.Item1.Motion == EventMotionKind.Attack
                ? ActivityVisualMeaning.Role(reference.Item1, reference.Item2)
                : ActivityVisualMeaning.LocalResult(reference.Item1, reference.Item2))));
        SetMeta("visible_result_count", references.Count);
        QueueRedraw();
    }

    public override void _Draw()
    {
        Rect2[] exclusions = Exclusions?.Invoke() ?? [];
        DrawConnections(exclusions);
        var rows = new Dictionary<int, int>();
        foreach (var reference in subjects)
            DrawReference(reference, rows, exclusions);
    }

    private void DrawConnections(IReadOnlyList<Rect2> exclusions)
    {
        foreach (var group in subjects.GroupBy(subject => subject.Entry))
        {
            var source = group.FirstOrDefault(subject => subject.Subject.Role is ActivitySubjectRole.Source or ActivitySubjectRole.Boost);
            var target = group.FirstOrDefault(subject => subject.Subject.Role == ActivitySubjectRole.Target);
            if (source.Subject is not null && target.Subject is not null && source.Bounds != target.Bounds)
                DrawImpulse(source.Bounds, target.Bounds, exclusions);
        }
    }

    private void DrawReference((EventPresentation Entry, ActivitySubject Subject, Rect2 Bounds) reference,
        Dictionary<int, int> rows, IReadOnlyList<Rect2> exclusions)
    {
        Rect2 rect = reference.Bounds;
        string role = ActivityVisualMeaning.Role(reference.Entry, reference.Subject);
        bool roleOnly = reference.Entry.Motion == EventMotionKind.Attack
            || reference.Subject.Role is ActivitySubjectRole.Source or ActivitySubjectRole.Defender;
        string result = roleOnly ? role : ActivityVisualMeaning.LocalResult(reference.Entry, reference.Subject);
        Color ink = ClientTheme.ToGodot(reference.Entry.Motion is EventMotionKind.Damage or EventMotionKind.Defeat
            ? VisualSystem.Palette.Danger : VisualSystem.Palette.Accent) with { A = emphasis };
        // A bracket names a result reference; it does not replace live counters or readiness.
        Vector2 corner = rect.Position;
        DrawLine(corner, corner + new Vector2(Math.Min(38, rect.Size.X), 0), ink, 3, true);
        DrawLine(corner, corner + new Vector2(0, 22), ink, 3, true);
        // Compact sources already expose their updated counters and state inline.
        // Their transient emphasis stays inside the tile, clear of the chronology.
        if (!ShowsBadge(reference.Entry, reference.Subject, rect, roleOnly)) return;
        int row = rows.GetValueOrDefault(reference.Subject.Card);
        rows[reference.Subject.Card] = row + 1;
        DrawBadge(reference, result, roleOnly, row, exclusions);
    }

    private bool ShowsBadge(EventPresentation entry, ActivitySubject subject, Rect2 bounds, bool roleOnly) =>
        bounds.Size.Y >= 110 && entry.Motion != EventMotionKind.State
        && !(entry.Motion == EventMotionKind.Attack && subject.Role == ActivitySubjectRole.Target)
        && (impacted || roleOnly);

    private void DrawBadge((EventPresentation Entry, ActivitySubject Subject, Rect2 Bounds) reference,
        string result, bool roleOnly, int row, IReadOnlyList<Rect2> exclusions)
    {
        Rect2 rect = reference.Bounds;
        Font font = CardTypography.Bold;
        float width = font.GetStringSize(result, fontSize: 16).X + 44;
        bool fits = width <= Math.Min(300, rect.Size.X);
        if (!fits) width = 30;
        // The badge occupies the exterior edge; printed rules and live values stay uncovered.
        Rect2 badge = new(rect.Position.X, Math.Max(2, rect.Position.Y - 32 - row * 32), width, 30);
        if (exclusions.Any(region => region.Intersects(badge))) return;
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

    private void DrawImpulse(Rect2 sourceBounds, Rect2 targetBounds, IReadOnlyList<Rect2> exclusions)
    {
        Color ink = ClientTheme.ToGodot(VisualSystem.Palette.Accent);
        Vector2 direction = (targetBounds.GetCenter() - sourceBounds.GetCenter()).Normalized();
        if (direction.IsZeroApprox()) return;
        Vector2 source = Edge(sourceBounds, direction) + direction * 5;
        Vector2 destination = Edge(targetBounds, -direction) - direction * 7;
        if ((destination - source).Dot(direction) <= 0) return;
        Vector2 tip = animated ? source.Lerp(destination, progress) : destination;
        DrawConnectorStroke(source, tip, CardFaceStyle.Ink with { A = .8f }, 4, exclusions);
        DrawConnectorStroke(source, tip, ink, 1.5f, exclusions);
        Vector2 normal = new(-direction.Y, direction.X);
        DrawConnectorStroke(tip, tip - direction * 9 + normal * 5, ink, 2, exclusions);
        DrawConnectorStroke(tip, tip - direction * 9 - normal * 5, ink, 2, exclusions);
        if (!exclusions.Any(region => region.HasPoint(source)))
            DrawCircle(source, 3, ink, antialiased: true);
    }

    private static Vector2 Edge(Rect2 bounds, Vector2 direction)
    {
        Vector2 half = bounds.Size / 2;
        float horizontal = Math.Abs(direction.X) < .001f ? float.MaxValue : half.X / Math.Abs(direction.X);
        float vertical = Math.Abs(direction.Y) < .001f ? float.MaxValue : half.Y / Math.Abs(direction.Y);
        return bounds.GetCenter() + direction * Math.Min(horizontal, vertical);
    }

    private void DrawConnectorStroke(Vector2 from, Vector2 to, Color ink, float width, IReadOnlyList<Rect2> exclusions)
    {
        foreach (var segment in ActivityConnectorGeometry.Segments(from, to, exclusions))
            DrawLine(segment.From, segment.To, ink, width, true);
    }

    internal Func<Rect2[]>? Exclusions { get; set; }
}
