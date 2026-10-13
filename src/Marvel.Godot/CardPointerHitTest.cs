using Godot;

namespace Marvel.Godot;

/// <summary>Target hit testing follows transformed card faces and every ancestor clip.</summary>
internal static class CardPointerHitTest
{
    internal static bool Contains(Control card, Vector2 point)
    {
        if (!InteractionControl.IsUsable(card) || !card.IsVisibleInTree()
            || !new Rect2(Vector2.Zero, card.Size).HasPoint(card.GetGlobalTransform().AffineInverse() * point)) return false;
        for (Node? ancestor = card.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
        {
            if (ancestor is Control clip && (clip is ScrollContainer || clip.ClipContents)
                && !new Rect2(Vector2.Zero, clip.Size).HasPoint(clip.GetGlobalTransform().AffineInverse() * point)) return false;
        }
        return true;
    }
}
