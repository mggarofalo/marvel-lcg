namespace Marvel.Godot;

/// <summary>Pure source-edge placement rules for the full-card inspector.</summary>
internal static class InspectorPositioning
{
    internal static InspectorPlacement Place(InspectorPlacementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);
        if (!FitsViewport(request))
        {
            return Fallback(request.ViewportWidth, request.ViewportHeight,
                request.PanelWidth, request.PanelHeight, request.Margin);
        }

        return request.HandSource ? HandPlacement(request) : BoardPlacement(request);
    }

    private static void Validate(InspectorPlacementRequest request)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.ViewportWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.ViewportHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SourceWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SourceHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.PanelWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.PanelHeight);
    }

    private static bool FitsViewport(InspectorPlacementRequest request) =>
        request.PanelWidth <= request.ViewportWidth - request.Margin * 2
        && request.PanelHeight <= request.ViewportHeight - request.Margin * 2;

    private static InspectorPlacement HandPlacement(InspectorPlacementRequest request)
    {
        int maximumX = request.ViewportWidth - request.PanelWidth - request.Margin;
        int above = request.SourceY - request.Gap - request.PanelHeight;
        return above >= request.Margin
            ? new InspectorPlacement(
                Math.Clamp(request.SourceX + request.SourceWidth / 2 - request.PanelWidth / 2,
                    request.Margin, maximumX),
                above,
                InspectorAttachment.Above)
            : Fallback(request.ViewportWidth, request.ViewportHeight,
                request.PanelWidth, request.PanelHeight, request.Margin);
    }

    private static InspectorPlacement BoardPlacement(InspectorPlacementRequest request)
    {
        int maximumX = request.ViewportWidth - request.PanelWidth - request.Margin;
        int maximumY = request.ViewportHeight - request.PanelHeight - request.Margin;
        int right = request.SourceX + request.SourceWidth + request.Gap;
        int left = request.SourceX - request.Gap - request.PanelWidth;
        bool rightFits = right <= maximumX;
        bool leftFits = left >= request.Margin;
        if (rightFits && (!leftFits
            || request.ViewportWidth - (request.SourceX + request.SourceWidth) >= request.SourceX))
        {
            return BoardPosition(request, right, maximumY, InspectorAttachment.Right);
        }

        if (leftFits)
        {
            return BoardPosition(request, left, maximumY, InspectorAttachment.Left);
        }

        return Fallback(request.ViewportWidth, request.ViewportHeight,
            request.PanelWidth, request.PanelHeight, request.Margin);
    }

    private static InspectorPlacement BoardPosition(
        InspectorPlacementRequest request, int x, int maximumY, InspectorAttachment attachment) =>
        new InspectorPlacement(
            x,
            Math.Clamp(request.SourceY + request.SourceHeight / 2 - request.PanelHeight / 2,
                request.Margin, maximumY),
            attachment);

    internal static InspectorPlacement Fallback(
        int viewportWidth, int viewportHeight, int panelWidth, int panelHeight, int margin)
    {
        int maximumX = Math.Max(margin, viewportWidth - panelWidth - margin);
        int maximumY = Math.Max(margin, viewportHeight - panelHeight - margin);
        return new InspectorPlacement(
            Math.Clamp((viewportWidth - panelWidth) / 2, margin, maximumX),
            Math.Clamp((viewportHeight - panelHeight) / 2, margin, maximumY),
            InspectorAttachment.ViewportFallback);
    }
}
