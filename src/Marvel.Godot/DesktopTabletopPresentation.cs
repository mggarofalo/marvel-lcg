namespace Marvel.Godot;

/// <summary>Routes the supported desktop viewport to the Astra spatial table.</summary>
/// <remarks>
/// This is a presentation-only arrangement of visibility-safe objects. Changing
/// the expanded seat never changes the prompt or its shared decision draft.
/// </remarks>
internal static class DesktopTabletopPresentation
{
    internal static BoardRenderResult Render(
        Main main,
        DisplayedSeatSelection selection,
        Action<int> switchSeat)
        => SpatialTableSurfaceRenderer.Render(
            main, selection, switchSeat, main.CurrentGame?.Prompt);
}
