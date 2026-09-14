using Godot;

namespace Marvel.Godot;

/// <summary>Determines whether a relationship endpoint remains inside every clipping surface.</summary>
internal static class RelationshipOverlayVisibility
{
    internal static bool EndpointIsVisible(
        Vector2 center,
        Rect2 viewport,
        IReadOnlyList<Rect2> clippingRects) =>
        viewport.HasPoint(center) && clippingRects.All(rect => rect.HasPoint(center));

    internal static bool EndpointIsVisible(Control endpoint, Rect2 viewport) =>
        endpoint.IsVisibleInTree() && EndpointIsVisible(
            VisualBounds(endpoint).GetCenter(), viewport, ClippingRects(endpoint));

    /// <summary>Returns the on-screen bounds after every enclosing scroll offset.</summary>
    internal static Rect2 VisualBounds(Control control)
    {
        Rect2 bounds = control.GetGlobalRect();
        for (Node? node = control.GetParent(); node is not null; node = node.GetParent())
        {
            if (node is ScrollContainer scroll)
            {
                bounds.Position -= new Vector2(scroll.ScrollHorizontal, scroll.ScrollVertical);
            }
        }

        return bounds;
    }

    internal static Rect2? VisibleBounds(Control control, Rect2 viewport)
    {
        Rect2 bounds = VisualBounds(control).Intersection(viewport);
        foreach (Rect2 clip in ClippingRects(control))
        {
            bounds = bounds.Intersection(clip);
        }

        return bounds.HasArea() ? bounds : null;
    }

    private static List<Rect2> ClippingRects(Control control)
    {
        var clips = new List<Rect2>();
        for (Node? node = control.GetParent(); node is not null; node = node.GetParent())
        {
            if (node is ScrollContainer scroll)
            {
                clips.Add(VisualBounds(scroll));
            }
            else if (node is Control { ClipContents: true } clip)
            {
                clips.Add(VisualBounds(clip));
            }
        }

        return clips;
    }
}
