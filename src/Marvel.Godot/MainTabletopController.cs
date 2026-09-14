using Godot;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Owns the presentation-only expanded-seat workspace on a desktop table.</summary>
internal sealed class MainTabletopController
{
    private readonly Main main;
    private DisplayedSeatState displayedSeats = new();

    internal MainTabletopController(Main main)
    {
        this.main = main;
    }

    internal void ResetForGame() => displayedSeats = new DisplayedSeatState();

    internal BoardRenderResult? Render(Prompt? prompt, Vector2 viewport)
    {
        if (prompt is { } opening && MulliganPrompt.UsesDesktopTable(opening, viewport))
        {
            return RenderMulligan(opening);
        }

        return DesktopTabletop.Uses(viewport) ? RenderDesktop(prompt) : null;
    }

    internal void FocusAnchors(IReadOnlyList<int> ids)
    {
        int? seat = AnchorSeat(ids);
        if (ShouldSwitchTo(seat))
        {
            SwitchSeat(seat!.Value);
        }

        main.boardRender?.Highlight(ids);
    }

    private BoardRenderResult RenderMulligan(Prompt prompt) =>
        MulliganTablePresentation.Render(
            main, prompt, Selection(prompt).ExpandedSeat, SwitchSeat);

    private BoardRenderResult RenderDesktop(Prompt? prompt) =>
        DesktopTabletopPresentation.Render(main, Selection(prompt).ExpandedSeat, SwitchSeat);

    private int? AnchorSeat(IReadOnlyList<int> ids) => main.boardPresentation?.Areas
            .Where(area => area.Seat >= 0 && area.Cards.Concat(area.Removed)
                .Any(card => card.TargetId is { } id && ids.Contains(id)))
            .Select(area => (int?)area.Seat)
            .FirstOrDefault();

    private bool ShouldSwitchTo(int? seat) => seat is not null
        && DesktopTabletop.Uses(main.GetViewportRect().Size)
        && seat != Selection(main.CurrentGame?.Prompt).ExpandedSeat;

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
