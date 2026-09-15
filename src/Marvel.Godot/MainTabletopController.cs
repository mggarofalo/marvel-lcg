using Godot;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Owns the presentation-only expanded-seat workspace on a desktop table.</summary>
internal sealed class MainTabletopController
{
    private readonly Main main;
    private DisplayedSeatState displayedSeats = new();
    private bool? renderedDesktopTabletop;
    private int? renderedExpandedSeat;

    internal MainTabletopController(Main main)
    {
        this.main = main;
    }

    internal void ResetForGame()
    {
        displayedSeats = new DisplayedSeatState();
        renderedDesktopTabletop = null;
        renderedExpandedSeat = null;
    }

    internal BoardRenderResult? Render(Prompt? prompt, Vector2 viewport)
    {
        bool desktop = DesktopTabletop.Uses(viewport);
        renderedDesktopTabletop = desktop;
        if (desktop && prompt is { } opening && MulliganPrompt.IsOpening(opening))
        {
            return RenderMulligan(opening);
        }

        return desktop ? RenderDesktop(prompt) : null;
    }

    internal void RerenderForViewport(Vector2 viewport)
    {
        if (main.board.Visible
            && DesktopTabletop.RouteChanged(renderedDesktopTabletop, viewport)
            && main.CurrentGame?.World is { } world)
        {
            ScrollContainer table = main.GetNode<ScrollContainer>(
                "Margin/Shell/Content/Play/Board/TableScroll");
            table.ScrollHorizontal = 0;
            table.ScrollVertical = 0;
            main.RenderBoard(world);
        }
    }

    internal void FocusAnchors(IReadOnlyList<int> ids)
    {
        int? seat = TabletopAnchorSeat.For(main.boardPresentation, ids);
        if (seat is not null && DesktopTabletop.Uses(main.GetViewportRect().Size))
        {
            FocusSeat(seat!.Value);
        }

        main.boardRender?.Highlight(ids);
    }

    private BoardRenderResult RenderMulligan(Prompt prompt) =>
        MulliganTablePresentation.Render(
            main, prompt, Selection(prompt).ExpandedSeat, SwitchSeat);

    private BoardRenderResult RenderDesktop(Prompt? prompt)
    {
        DisplayedSeatSelection selection = Selection(prompt);
        renderedExpandedSeat = selection.ExpandedSeat;
        return DesktopTabletopPresentation.Render(main, selection, SwitchSeat);
    }

    private void SwitchSeat(int seat)
    {
        if (main.boardPresentation?.Lanes.Any(lane => lane.Seat == seat) != true)
        {
            return;
        }

        // This changes only the expanded public workspace. The pending prompt
        // and its composer remain owned by the server-provided prompt player.
        displayedSeats.Select(seat, Snapshot(main.CurrentGame?.Prompt));
        if (main.CurrentGame?.World is { } world)
        {
            main.RenderBoard(world);
        }
    }

    private void FocusSeat(int seat)
    {
        if (main.boardPresentation?.Lanes.Any(lane => lane.Seat == seat) != true
            || seat == renderedExpandedSeat)
        {
            return;
        }

        displayedSeats.Focus(seat, Snapshot(main.CurrentGame?.Prompt));
        if (main.CurrentGame?.World is { } world)
        {
            main.RenderBoard(world);
        }
    }

    private DisplayedSeatSelection Selection(Prompt? prompt) =>
        displayedSeats.Update(Snapshot(prompt));

    private DisplayedSeatSnapshot Snapshot(Prompt? prompt)
    {
        Marvel.View.BoardPresentation board = main.boardPresentation!;
        int[] seats = [.. board.Lanes
            .Where(lane => lane.Seat is not null)
            .Select(lane => lane.Seat!.Value)
            .Concat(board.PlayerSummaries.Select(summary => summary.Seat))
            .Distinct()
            .OrderBy(seat => seat)];
        return new DisplayedSeatSnapshot(
            main.CurrentGame?.GameId ?? "active-game",
            seats,
            new DisplayedSeatRoles(
                prompt?.Player ?? board.Table?.PromptOwner,
                board.Table?.ViewedPrivateSeat,
                board.Table?.ActivePlayer,
                board.Table?.PublicFocusSeat));
    }
}
