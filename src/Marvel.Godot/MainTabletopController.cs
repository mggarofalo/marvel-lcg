using Godot;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Owns the presentation-only expanded-seat workspace on a desktop table.</summary>
internal sealed class MainTabletopController
{
    private readonly Main main;
    private int? displayedSeat;

    internal MainTabletopController(Main main)
    {
        this.main = main;
    }

    internal void ResetForGame() => displayedSeat = null;

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
        MulliganTablePresentation.Render(main, prompt, displayedSeat ?? prompt.Player, SwitchSeat);

    private BoardRenderResult RenderDesktop(Prompt? prompt) =>
        DesktopTabletopPresentation.Render(main, displayedSeat ?? InitialSeat(prompt), SwitchSeat);

    private int InitialSeat(Prompt? prompt) => prompt?.Player
        ?? (main.boardPresentation!.PlayerSummaries.Count > 0
            ? main.boardPresentation.PlayerSummaries[0].Seat
            : 0);

    private int? AnchorSeat(IReadOnlyList<int> ids) => main.boardPresentation?.Areas
            .Where(area => area.Seat >= 0 && area.Cards.Concat(area.Removed)
                .Any(card => card.TargetId is { } id && ids.Contains(id)))
            .Select(area => (int?)area.Seat)
            .FirstOrDefault();

    private bool ShouldSwitchTo(int? seat) => seat is not null
        && DesktopTabletop.Uses(main.GetViewportRect().Size)
        && displayedSeat != seat;

    private void SwitchSeat(int seat)
    {
        if (main.boardPresentation?.Lanes.Any(lane => lane.Seat == seat) != true
            || displayedSeat == seat)
        {
            return;
        }

        // This changes only the expanded public workspace. The pending prompt
        // and its composer remain owned by the server-provided prompt player.
        displayedSeat = seat;
        if (main.CurrentGame?.World is { } world)
        {
            main.RenderBoard(world);
        }
    }
}
