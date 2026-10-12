using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Keeps a receipt's authorized printed source available from its title.</summary>
internal static class AbilitySourceInspection
{
    internal static void Bind(Main main, Control title, BoardCardPresentation face)
    {
        title.FocusMode = Control.FocusModeEnum.All;
        title.MouseFilter = Control.MouseFilterEnum.Stop;
        title.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        title.TooltipText = "Inspect " + face.Title;
        void Preview()
        {
            if (!main.cardInspectorPinned) main.boardController.ShowCardInspector(face, title, false);
        }
        title.MouseEntered += Preview;
        title.MouseExited += main.boardController.ScheduleCardInspectorHide;
        CardFocusPreview.Bind(title, Preview, main.boardController.ScheduleCardInspectorHide);
        title.GuiInput += input =>
        {
            if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
                || input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Enter or Key.Space })
            {
                main.boardController.ShowCardInspector(face, title, true);
                title.AcceptEvent();
            }
        };
    }
}
