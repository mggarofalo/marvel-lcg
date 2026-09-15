using Godot;
using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders the persistent desktop table as stable far-side and near-side rails.</summary>
/// <remarks>
/// This is a presentation-only arrangement of visibility-safe areas. Changing the
/// expanded seat never changes the prompt or its shared decision draft.
/// </remarks>
internal static class DesktopTabletopPresentation
{
    internal static BoardRenderResult Render(
        Main main,
        DisplayedSeatSelection selection,
        Action<int> switchSeat)
    {
        PanelContainer shell = main.GetNode<PanelContainer>("Margin/Shell");
        shell.CustomMinimumSize = new Vector2(
            Math.Max(0, main.GetViewportRect().Size.X - 32),
            shell.CustomMinimumSize.Y);
        BoardRenderCleanup.Clear(main.boardAreas);
        BoardRenderCleanup.Clear(main.handRail);
        BoardPresentation board = main.boardPresentation!;
        var result = new BoardRenderResult();
        InterfaceScale tableScale = main.interfaceScale > InterfaceScale.Standard
            ? InterfaceScale.Standard
            : main.interfaceScale;
        TabletopRailPlan plan = TabletopRailPlan.Create(board.Lanes, selection.ExpandedSeat);
        main.boardAreas.AddChild(TabletopRailRenderer.Rail(
            "VillainTable", "VILLAIN TABLE  ·  FAR SIDE", [.. plan.FarLive, .. plan.FarShelf],
            result, tableScale, main.art));
        PanelContainer playerTable = TabletopRailRenderer.Rail(
            "PlayerTable", $"PLAYER {selection.ExpandedSeat + 1}  ·  NEAR SIDE",
            [.. plan.NearLive, .. plan.NearShelf],
            result, tableScale, main.art, selection.ExpandedSeat);
        if (board.PlayerSummaries.Count > 1)
        {
            TabletopRailRenderer.ReplaceHeading(
                playerTable, MulliganSeatStripRenderer.Create(board, selection, switchSeat));
        }
        main.boardAreas.AddChild(playerTable);
        TabletopHandShelfRenderer.Render(
            board, selection.ExpandedSeat, main.handRail, main.handHeading,
            result, tableScale, main.art);
        main.GetNode<PanelContainer>("Margin/Shell/Content/Play/Board/HandShelf").Visible =
            main.CurrentGame?.World?.Outcome is not { } outcome || outcome == Outcome.Unfinished;
        return result;
    }
}
