using Godot;

namespace Marvel.Godot;

/// <summary>Draws interaction cues without replacing the card's aspect frame.</summary>
internal static class CardInteractionDrawing
{
    internal static void Draw(Control card, CardInteractionCue cue, bool presented)
    {
        bool selected = (cue & (CardInteractionCue.SelectedTarget | CardInteractionCue.SelectedDestructiveChoice)) != 0;
        DrawOutline(card, cue, presented, selected);
        if (cue.HasFlag(CardInteractionCue.SelectedGenerator) && card.HasMeta("card_resource_rect"))
            StrokeRect(card, card.GetMeta("card_resource_rect").AsRect2().Grow(2), Colors.White, 2);
        if (!selected) return;
        const float length = 16;
        foreach (Vector2 corner in new[] { Vector2.Zero, new Vector2(card.Size.X, 0),
            card.Size, new Vector2(0, card.Size.Y) })
        {
            Vector2 inward = new(corner.X == 0 ? 1 : -1, corner.Y == 0 ? 1 : -1);
            card.DrawLine(corner, corner + new Vector2(length * inward.X, 0), Colors.White, 4, antialiased: true);
            card.DrawLine(corner, corner + new Vector2(0, length * inward.Y), Colors.White, 4, antialiased: true);
        }
    }

    private static void DrawOutline(Control card, CardInteractionCue cue, bool presented, bool selected)
    {
        bool available = (cue & (CardInteractionCue.OfferedAction | CardInteractionCue.LegalTarget
            | CardInteractionCue.LegalGenerator | CardInteractionCue.DestructiveChoice)) != 0;
        bool target = cue.HasFlag(CardInteractionCue.LegalTarget) || cue.HasFlag(CardInteractionCue.SelectedTarget);
        if (target)
        {
            StrokeRect(card, new Rect2(Vector2.Zero, card.Size),
                ClientTheme.ToGodot(selected ? VisualSystem.Palette.Selected : VisualSystem.Palette.Legal), 3);
        }
        else if ((available || presented) && !selected && !card.HasFocus())
        {
            StrokeRect(card, new Rect2(Vector2.Zero, card.Size), new Color(0.95f, 0.98f, 1, 0.85f), 1.5f);
        }
    }

    internal static void StrokeRect(Control card, Rect2 rect, Color color, float width)
    {
        Rect2 outer = rect.Grow(width / 2), inner = rect.Grow(-width / 2);
        Vector2[] outside = [outer.Position, new(outer.End.X, outer.Position.Y), outer.End,
            new(outer.Position.X, outer.End.Y)];
        Vector2[] inside = [inner.Position, new(inner.End.X, inner.Position.Y), inner.End,
            new(inner.Position.X, inner.End.Y)];
        // One solid ring and one coverage fringe per boundary keep diagonal coverage uniform.
        for (int edge = 0; edge < outside.Length; edge++)
        {
            int next = (edge + 1) % outside.Length;
            card.DrawColoredPolygon([outside[edge], outside[next], inside[next], inside[edge]], color);
        }
        CardEdgeSmoothing.Draw(card, outside, color);
        Array.Reverse(inside);
        CardEdgeSmoothing.Draw(card, inside, color);
    }
}
