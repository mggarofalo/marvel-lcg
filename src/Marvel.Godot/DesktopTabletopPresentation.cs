using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders the persistent desktop table around one expanded player workspace.</summary>
/// <remarks>
/// This is a presentation-only selection of already-projected lanes. A seat switch
/// never changes the prompt or draft; it only chooses which public workspace is open.
/// </remarks>
internal static class DesktopTabletopPresentation
{
    internal static BoardRenderResult Render(
        Main main,
        int expandedSeat,
        Action<int> switchSeat)
    {
        BoardRenderCleanup.Clear(main.boardAreas);
        BoardRenderCleanup.Clear(main.handRail);
        BoardPresentation board = main.boardPresentation!;
        var result = new BoardRenderResult();
        IReadOnlyList<BoardLanePresentation> lanes = board.Lanes;

        AddLane("scenario");
        AddLane("other");
        if (board.PlayerSummaries.Count > 1)
        {
            main.boardAreas.AddChild(MulliganSeatStripRenderer.Create(
                board, expandedSeat, switchSeat));
        }

        BoardLanePresentation? expanded = lanes.FirstOrDefault(lane => lane.Seat == expandedSeat);
        if (expanded is not null)
        {
            main.boardAreas.AddChild(BoardRenderer.Lane(
                WithoutHand(expanded), result, main.interfaceScale, main.expandedAreas, main.art));
        }

        BoardRenderer.RenderHand(
            board, main.handRail, main.handHeading, result, main.interfaceScale, main.art, expandedSeat);
        return result;

        void AddLane(string key)
        {
            BoardLanePresentation? lane = lanes.FirstOrDefault(item => item.Key == key);
            if (lane is not null)
            {
                main.boardAreas.AddChild(BoardRenderer.Lane(
                    WithoutHand(lane), result, main.interfaceScale, main.expandedAreas, main.art));
            }
        }
    }

    private static BoardLanePresentation WithoutHand(BoardLanePresentation lane) => lane with
    {
        Areas = [.. lane.Areas.Where(area => area.Zone != "HandsArea")],
    };
}
