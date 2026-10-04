using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns physical face sizes and their rotation-safe table envelope.</summary>
internal static class SpatialCardMetrics
{
    internal static InterfaceScale TableScale(InterfaceScale scale) =>
        scale > InterfaceScale.Percent110 ? InterfaceScale.Percent110 : scale;

    internal static Vector2 FaceSize(
        BoardCardPresentation card, CardDisplaySize size, CardLayoutMetrics layout, InterfaceScale scale)
    {
        float cueHeight = 22 * layout.Width / 144.0f;
        float stripHeight = size is CardDisplaySize.Hand or CardDisplaySize.Mulligan
            ? Math.Max(cueHeight, VisualSystem.Controls(scale).MinimumPointerTarget) : cueHeight;
        float height = card.Concealed ? layout.Width * 0.72f
            : size == CardDisplaySize.Full ? layout.MinimumHeight
            : CardControl.CompactHeight(card, layout, size) + stripHeight - cueHeight;
        return new Vector2(layout.Width, height);
    }

    internal static Vector2 Envelope(
        IEnumerable<BoardCardPresentation> cards, CardDisplaySize size, InterfaceScale scale)
    {
        CardLayoutMetrics fallback = VisualSystem.Card(size, scale);
        Vector2 maximum = new(fallback.Width, fallback.MinimumHeight);
        foreach (BoardCardPresentation card in cards)
        {
            Vector2 face = FaceSize(card, size, CardControl.LayoutFor(card, size, scale), scale);
            maximum = new Vector2(Math.Max(maximum.X, face.X), Math.Max(maximum.Y, face.Y));
        }
        return maximum;
    }
}
