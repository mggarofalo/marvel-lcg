using Godot;

namespace Marvel.Godot;

/// <summary>Draws interaction cues without replacing the card's aspect frame.</summary>
internal static class CardInteractionDrawing
{
    internal static void Draw(Control card, CardInteractionCue cue, bool presented)
    {
        bool selected = (cue & (CardInteractionCue.SelectedTarget | CardInteractionCue.SelectedDestructiveChoice)) != 0;
        bool available = (cue & (CardInteractionCue.OfferedAction | CardInteractionCue.LegalTarget
            | CardInteractionCue.LegalGenerator | CardInteractionCue.DestructiveChoice)) != 0;
        if ((available || presented) && !selected && !card.HasFocus())
        {
            card.DrawRect(new Rect2(new Vector2(-2, -2), card.Size + new Vector2(4, 4)),
                new Color(1, 1, 1, 0.2f), false, 6);
            card.DrawRect(new Rect2(Vector2.Zero, card.Size), new Color(0.95f, 0.98f, 1, 0.85f), false, 1.5f);
        }
        if (cue.HasFlag(CardInteractionCue.SelectedGenerator) && card.HasMeta("card_resource_rect"))
            card.DrawRect(card.GetMeta("card_resource_rect").AsRect2().Grow(2), Colors.White, false, 2);
        if (!selected) return;
        const float length = 16;
        foreach (Vector2 corner in new[] { Vector2.Zero, new Vector2(card.Size.X, 0),
            card.Size, new Vector2(0, card.Size.Y) })
        {
            Vector2 inward = new(corner.X == 0 ? 1 : -1, corner.Y == 0 ? 1 : -1);
            card.DrawLine(corner, corner + new Vector2(length * inward.X, 0), Colors.White, 4);
            card.DrawLine(corner, corner + new Vector2(0, length * inward.Y), Colors.White, 4);
        }
    }
}
