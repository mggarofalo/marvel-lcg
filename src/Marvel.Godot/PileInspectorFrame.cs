using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>One native-type-size inspection frame reserves both card orientations for a whole pile.</summary>
internal static class PileInspectorFrame
{
    internal static InterfaceScale Configure(PopupPanel popup, Control source,
        IReadOnlyList<BoardCardPresentation> cards, InterfaceScale requested)
    {
        Vector2 viewport = source.GetViewportRect().Size;
        InterfaceScale scale = cards.Select(card => CardInspectorFocus.FittedScale(card, requested, viewport.Y - 96))
            .Min();
        CardLayoutMetrics[] layouts = [.. cards.Select(card => CardControl.LayoutFor(card, CardDisplaySize.Full, scale))];
        Vector2 size = new(Math.Min(viewport.X - 24, layouts.Max(layout => layout.Width) + 312),
            Math.Min(viewport.Y - 24, layouts.Max(layout => layout.MinimumHeight) + 148));
        Rect2 frame = CardInspectorPlacement.Fit(viewport, SpatialCardFootprint.Face(source), size);
        popup.MinSize = new Vector2I(Mathf.CeilToInt(frame.Size.X), Mathf.CeilToInt(frame.Size.Y));
        popup.Size = popup.MinSize;
        popup.Position = new Vector2I(Mathf.RoundToInt(frame.Position.X), Mathf.RoundToInt(frame.Position.Y));
        return scale;
    }
}
