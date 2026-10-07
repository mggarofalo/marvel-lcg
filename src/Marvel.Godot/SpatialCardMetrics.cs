using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns physical face sizes and their rotation-safe table envelope.</summary>
internal static class SpatialCardMetrics
{
    internal static InterfaceScale TableScale(InterfaceScale scale, float tableHeight = AstraTableGeometry.ReferenceHeight)
    {
        int maximum = Math.Clamp((int)(100 * tableHeight / AstraTableGeometry.ReferenceHeight / 10) * 10, 80, 100);
        return (InterfaceScale)Math.Min(Math.Max((int)scale, 80), maximum);
    }

    internal static Vector2 FaceSize(
        BoardCardPresentation card, CardDisplaySize size, CardLayoutMetrics layout, InterfaceScale scale)
    {
        float height = layout.MinimumHeight;
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
