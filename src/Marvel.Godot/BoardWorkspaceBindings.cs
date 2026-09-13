using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Connects presentation-only workspace controls to the current draft and rerender.</summary>
internal static class BoardWorkspaceBindings
{
    internal static void Bind(
        Main main,
        BoardRenderResult result,
        WorldDescriptor world,
        Action<WorldDescriptor> render)
    {
        result.ViewedSeatRequested += (seat, _) => SelectSeat(main, seat, world, render);
        result.AffordanceRequested += id =>
        {
            main.decisions.SelectAffordanceFromBoard(id);
            render(world);
        };
        result.TargetRequested += id =>
        {
            main.decisions.ToggleOrdinaryTargetFromBoard(id);
            render(world);
        };
        result.RefreshRequested += focusName => Refresh(main, world, focusName, render);
    }

    private static void Refresh(
        Main main, WorldDescriptor world, string focusName, Action<WorldDescriptor> render)
    {
        render(world);
        Callable.From(() =>
            (main.board.FindChild(focusName, recursive: true, owned: false) as Control)?
                .GrabFocus()).CallDeferred();
    }

    private static void SelectSeat(
        Main main, int seat, WorldDescriptor world, Action<WorldDescriptor> render)
    {
        main.viewedSeat = seat;
        render(world);
        Callable.From(() => main.boardRender?.SeatControlFor(seat)?.GrabFocus()).CallDeferred();
    }
}
