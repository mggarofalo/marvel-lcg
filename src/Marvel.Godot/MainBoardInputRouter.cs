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
        if (CardContextMenu.RouteInput()) return;
        inspector.ObservePreviewPointer(input);
        if (main.cardInspector.Visible && main.cardInspectorPinned)
        {
            inspector.Input(input);
            return;
        }
        if (main.decisions.CompleteChoicesOpen && !main.decisions.LocalDecision.IsOpen)
        {
            main.decisions.RouteDecisionSurfaceInput(input);
            return;
        }

        main.boardController.RouteAbilityNotificationInput(input);
        if (main.GetViewport().IsInputHandled()) return;
        if (RouteDecisionSurface(main, input)) return;

        if (BoardActionChoiceSurface.RouteInput(main.GetViewport(), input))
        {
            return;
        }

        if (RouteBoardPointer(main, inspector, input)) return;

        inspector.Input(input);
    }

    private static bool RouteDecisionSurface(Main main, InputEvent input)
    {
        if (!main.decisions.LocalDecision.IsOpen && !main.decisions.AbilityWindowOpen && !main.decisions.PaymentModalOpen) return false;
        main.decisions.RouteDecisionSurfaceInput(input);
        return main.GetViewport().IsInputHandled();
    }

    private static bool RouteBoardPointer(Main main, CardInspectorFocus inspector, InputEvent input)
    {
        if (main.boardRender?.RoutePointer(input) != true) return false;
        if (input is InputEventMouseMotion) inspector.Hide();
        main.GetViewport().SetInputAsHandled();
        return true;
    }

}
