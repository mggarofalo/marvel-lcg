using Godot;

namespace Marvel.Godot;

/// <summary>Routes root input between the board-owned pointer and the card inspector.</summary>
internal static class MainBoardInputRouter
{
    internal static void Route(Main main, CardInspectorFocus inspector, InputEvent input)
    {
        if (main.boardRender?.RoutePointer(input) == true)
        {
            main.GetViewport().SetInputAsHandled();
            return;
        }

        inspector.Input(input);
    }
}
