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
        Vector2 inset = popup.GetThemeStylebox("panel").GetMinimumSize();
        InterfaceScale scale = FitCards(cards, requested, viewport.Y - 24 - inset.Y - 148);
        CardLayoutMetrics[] layouts = [.. cards.Select(card => CardControl.LayoutFor(card, CardDisplaySize.Full, scale))];
        Vector2 size = new(Math.Min(viewport.X - 24, layouts.Max(layout => layout.Width) + 312 + inset.X),
            Math.Min(viewport.Y - 24, layouts.Max(layout => layout.MinimumHeight) + 148 + inset.Y));
        Rect2 frame = CardInspectorPlacement.Fit(viewport, SpatialCardFootprint.Face(source), size);
        // PopupPanel adds its themed inset to the requested content minimum when opening.
        popup.MinSize = new Vector2I(Mathf.CeilToInt(frame.Size.X - inset.X), Mathf.CeilToInt(frame.Size.Y - inset.Y));
        popup.Size = popup.MinSize;
        popup.Position = new Vector2I(Mathf.RoundToInt(frame.Position.X), Mathf.RoundToInt(frame.Position.Y));
        popup.SizeChanged += () => RefitMeasured(popup, source);
        return scale;
    }

    private static InterfaceScale FitCards(IReadOnlyList<BoardCardPresentation> cards,
        InterfaceScale requested, float availableHeight)
    {
        int scale = (int)requested;
        while (scale > 50 && cards.Any(card =>
            CardControl.LayoutFor(card, CardDisplaySize.Full, (InterfaceScale)scale).MinimumHeight > availableHeight))
            scale -= 10;
        return (InterfaceScale)scale;
    }

    private static void RefitMeasured(PopupPanel popup, Control source)
    {
        if (!GodotObject.IsInstanceValid(popup) || popup.IsQueuedForDeletion()
            || !InteractionControl.IsUsable(source)) return;
        Vector2 minimum = popup.GetContentsMinimumSize();
        Vector2 actual = new(Math.Max(popup.Size.X, minimum.X), Math.Max(popup.Size.Y, minimum.Y));
        Rect2 frame = CardInspectorPlacement.Fit(source.GetViewportRect().Size,
            SpatialCardFootprint.Face(source), actual);
        popup.Position = new Vector2I(Mathf.RoundToInt(frame.Position.X), Mathf.RoundToInt(frame.Position.Y));
    }
}
