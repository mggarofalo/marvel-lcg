using Godot;

namespace Marvel.Godot;

/// <summary>Routes root input between the board-owned pointer and the card inspector.</summary>
internal static class MainBoardInputRouter
{
    internal static void Route(
        Main main,
        CardInspectorFocus inspector,
        CardInspectorCardNavigation cards,
        InputEvent input)
    {
        if (main.decisions.PaymentModalOpen || main.decisions.CompleteChoicesOpen)
        {
            main.decisions.RouteDecisionSurfaceInput(input);
            return;
        }
        if (main.cardInspector.Visible && main.cardInspectorPinned)
        {
            if (cards.Route(input))
            {
                main.GetViewport().SetInputAsHandled();
                return;
            }

            inspector.Input(input);
            return;
        }

        if (main.boardRender?.RoutePointer(input) == true)
        {
            main.GetViewport().SetInputAsHandled();
            return;
        }

        if (cards.Route(input))
        {
            main.GetViewport().SetInputAsHandled();
            return;
        }

        inspector.Input(input);
    }
}
