using Godot;

namespace Marvel.Godot;

/// <summary>Positions the card inspector against a live source control or an explicit fallback.</summary>
internal static class CardInspectorPlacement
{
    private const int Margin = 12;
    private const int Gap = 12;

    internal static InspectorPlacement Position(
        Main main, Control detail, Control? source, bool pinned)
    {
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(detail);

        Vector2 detailSize = detail.GetCombinedMinimumSize();
        int viewportWidth = Math.Max(1, Mathf.RoundToInt(main.Size.X));
        int viewportHeight = Math.Max(1, Mathf.RoundToInt(main.Size.Y));
        int width = Mathf.RoundToInt(detailSize.X);
        int height = Math.Min(
            Math.Max(1, viewportHeight - Margin * 2),
            Mathf.RoundToInt(detailSize.Y));
        bool liveSource = source is not null && IsLive(source);
        bool handSource = liveSource && IsHandSource(source!);
        Rect2 sourceRect = liveSource ? source!.GetGlobalRect() : default;
        if (handSource && (!pinned || main.interfaceScale <= InterfaceScale.Standard))
        {
            // A hand popover spends only the vertical room above its source.
            // The inspector's existing scrollbar-free content contract still
            // governs what can shrink within that assigned frame.
            height = Math.Min(height, Math.Max(160,
                Mathf.RoundToInt(sourceRect.Position.Y) - Gap - Margin));
        }
        main.cardInspectorFrame.CustomMinimumSize = Vector2.Zero;
        main.cardInspectorFrame.Size = new Vector2(width, height);

        InspectorPlacement placement = liveSource
            ? VisualSystem.PlaceAnchoredInspector(new InspectorPlacementRequest(
                viewportWidth,
                viewportHeight,
                Mathf.RoundToInt(sourceRect.Position.X),
                Mathf.RoundToInt(sourceRect.Position.Y),
                Mathf.RoundToInt(sourceRect.Size.X),
                Mathf.RoundToInt(sourceRect.Size.Y),
                width,
                height,
                handSource,
                Margin,
                Gap))
            : InspectorPositioning.Fallback(viewportWidth, viewportHeight, width, height, Margin);
        main.cardInspectorFrame.Position = new Vector2(placement.X, placement.Y);
        UpdateConnector(main, source, placement);
        return placement;
    }

    internal static bool IsLive(Control source) =>
        GodotObject.IsInstanceValid(source)
        && source.IsInsideTree()
        && !source.IsQueuedForDeletion()
        && source.IsVisibleInTree();

    private static bool IsHandSource(Control source)
    {
        for (Node? current = source; current is not null; current = current.GetParent())
        {
            if (current.Name.ToString().StartsWith("HandCardSlot", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void UpdateConnector(
        Main main, Control? source, InspectorPlacement placement)
    {
        if (!placement.IsAttached || source is null || !IsLive(source))
        {
            Connector(main).Visible = false;
            return;
        }

        Rect2 sourceRect = source.GetGlobalRect();
        Rect2 frameRect = main.cardInspectorFrame.GetGlobalRect();
        (Vector2 sourceEdge, Vector2 frameEdge) = placement.Attachment switch
        {
            InspectorAttachment.Right => (
                new Vector2(sourceRect.End.X, sourceRect.GetCenter().Y),
                new Vector2(frameRect.Position.X, ClampY(frameRect, sourceRect.GetCenter().Y))),
            InspectorAttachment.Left => (
                new Vector2(sourceRect.Position.X, sourceRect.GetCenter().Y),
                new Vector2(frameRect.End.X, ClampY(frameRect, sourceRect.GetCenter().Y))),
            InspectorAttachment.Above => (
                new Vector2(sourceRect.GetCenter().X, sourceRect.Position.Y),
                new Vector2(ClampX(frameRect, sourceRect.GetCenter().X), frameRect.End.Y)),
            _ => throw new ArgumentOutOfRangeException(nameof(placement)),
        };
        Connector(main).Points = [sourceEdge, frameEdge];
        Connector(main).Visible = true;
    }

    private static float ClampY(Rect2 rectangle, float value) =>
        Mathf.Clamp(value, rectangle.Position.Y, rectangle.End.Y);

    private static float ClampX(Rect2 rectangle, float value) =>
        Mathf.Clamp(value, rectangle.Position.X, rectangle.End.X);

    internal static Line2D Connector(Main main) =>
        main.cardInspector.GetNode<Line2D>("Connector");
}
