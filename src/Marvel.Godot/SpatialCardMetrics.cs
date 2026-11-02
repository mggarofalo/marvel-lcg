using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns physical face sizes and their rotation-safe table envelope.</summary>
internal static class SpatialCardMetrics
{
    /// <summary>
    /// The table's card scale: the reading scale, at most 100%, on a table of the reference height. A shorter
    /// table shrinks cards to no less than 80%; a taller one grows them in proportion, to at most 150%, so a
    /// large window is filled by larger cards rather than empty space. These bounds are our choices.
    /// </summary>
    internal static InterfaceScale TableScale(InterfaceScale scale, float tableHeight = AstraTableGeometry.ReferenceHeight)
    {
        int reading = Math.Clamp((int)scale, 80, 100);
        if (tableHeight > AstraTableGeometry.ReferenceHeight)
            return (InterfaceScale)Math.Clamp(
                (int)MathF.Floor(reading * tableHeight / AstraTableGeometry.ReferenceHeight / 10) * 10, 80, 150);
        int maximum = Math.Clamp((int)MathF.Ceiling(100 * tableHeight / AstraTableGeometry.ReferenceHeight / 10) * 10, 80, 100);
        return (InterfaceScale)Math.Min(reading, maximum);
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
