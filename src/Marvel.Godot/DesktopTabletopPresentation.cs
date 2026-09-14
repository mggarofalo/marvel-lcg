using Godot;
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
        BoardRenderCleanup.Clear(main.boardAreas);
        BoardRenderCleanup.Clear(main.handRail);
        BoardPresentation board = main.boardPresentation!;
        var result = new BoardRenderResult();
        TabletopRailPlan plan = TabletopRailPlan.Create(board.Lanes, selection.ExpandedSeat);
        main.boardAreas.AddChild(TabletopRailRenderer.Rail(
            "VillainTable", "VILLAIN TABLE  ·  FAR SIDE", plan.FarLive,
            result, main.interfaceScale, main.art));
        AddShelf("ScenarioShelf", "SCENARIO SUPPORT", plan.FarShelf);
        if (board.PlayerSummaries.Count > 1)
        {
            main.boardAreas.AddChild(MulliganSeatStripRenderer.Create(board, selection, switchSeat));
        }
        main.boardAreas.AddChild(TabletopRailRenderer.Rail(
            "PlayerTable", $"PLAYER {selection.ExpandedSeat + 1}  ·  NEAR SIDE", plan.NearLive,
            result, main.interfaceScale, main.art, selection.ExpandedSeat));
        AddShelf("PlayerShelf", "PLAYER SUPPORT", plan.NearShelf);
        TabletopHandShelfRenderer.Render(
            board, selection.ExpandedSeat, main.handRail, main.handHeading,
            result, main.interfaceScale, main.art);
        return result;

        void AddShelf(string name, string title, IReadOnlyList<BoardAreaPresentation> areas)
        {
            if (areas.Count > 0)
            {
                main.boardAreas.AddChild(TabletopRailRenderer.Shelf(
                    name, title, areas, result, main.interfaceScale, main.art));
            }
        }
    }
}
