using Godot;

namespace Marvel.Godot;

/// <summary>Routes root input between the board-owned pointer and the card inspector.</summary>
internal static class MainBoardInputRouter
{
    internal static void Route(
        Main main,
        CardInspectorFocus inspector,
        InputEvent input)
    {
        inspector.ObservePreviewPointer(input);
        if (main.decisions.CompleteChoicesOpen)
        {
            main.decisions.RouteDecisionSurfaceInput(input);
            return;
        }
        if (main.cardInspector.Visible && main.cardInspectorPinned)
        {
            inspector.Input(input);
            return;
        }

        if (main.decisions.PaymentModalOpen)
        {
            main.decisions.RouteDecisionSurfaceInput(input);
            if (main.GetViewport().IsInputHandled()) return;
        }

        if (BoardActionChoiceSurface.RouteInput(main.GetViewport(), input))
        {
            return;
        }

        if (RouteBoardPointer(main, inspector, input)) return;

        inspector.Input(input);
    }

    private static bool RouteBoardPointer(Main main, CardInspectorFocus inspector, InputEvent input)
    {
        if (main.boardRender?.RoutePointer(input) != true) return false;
        if (input is InputEventMouseMotion) inspector.Hide();
        main.GetViewport().SetInputAsHandled();
        return true;
    }

}
