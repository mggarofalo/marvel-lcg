using Marvel.View;

namespace Marvel.Godot;

/// <summary>Captures the current client presentation state for a board render pass.</summary>
internal static class BoardRenderRequestFactory
{
    internal static BoardRenderRequest Create(Main main, BoardPresentation board) => new(
        main.boardAreas,
        board,
        main.handRail,
        main.handHeading,
        main.interfaceScale,
        main.expandedAreas,
        main.boardPages,
        main.viewedSeat,
        main.CurrentGame?.Prompt,
        main.decisions.SelectedAffordanceId,
        main.decisions.SelectedTargets,
        main.art);
}
