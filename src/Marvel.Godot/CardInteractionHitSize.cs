using Godot;

namespace Marvel.Godot;

/// <summary>Physical collection scaling does not shrink pointer controls below their usable hit size.</summary>
internal static class CardInteractionHitSize
{
    internal static void Preserve(CardControl card, Button button)
    {
        if (card.HasMeta("source_tableau_tile")) return;
        Vector2 scale = card.GetGlobalTransform().Scale.Abs();
        if (scale.X <= 0 || scale.Y <= 0 || scale.X >= 1 && scale.Y >= 1) return;
        button.CustomMinimumSize = new Vector2(
            Math.Max(button.CustomMinimumSize.X, 44 / scale.X),
            Math.Max(button.CustomMinimumSize.Y, 44 / scale.Y));
    }
}
