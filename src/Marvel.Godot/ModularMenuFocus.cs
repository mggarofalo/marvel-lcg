using Godot;

namespace Marvel.Godot;

/// <summary>Keeps setup-menu focus inside its popup and returns it to the current trigger.</summary>
internal sealed class ModularMenuFocus
{
    private readonly Main main;
    private int generation;

    internal ModularMenuFocus(Main main)
    {
        this.main = main;
    }

    internal static void Bind(MenuButton modular, Main main)
    {
        var focus = new ModularMenuFocus(main);
        modular.GetPopup().AboutToPopup += focus.Focus;
        modular.GetPopup().PopupHide += focus.Restore;
    }

    internal void Focus()
    {
        int token = checked(++generation);
        Callable.From(() =>
        {
            PopupMenu popup = main.modular.GetPopup();
            if (token == generation && GodotObject.IsInstanceValid(popup)
                && popup.IsInsideTree() && !popup.IsQueuedForDeletion() && popup.Visible)
            {
                popup.GrabFocus();
            }
        }).CallDeferred();
    }

    internal void Restore()
    {
        int token = checked(++generation);
        Callable.From(() =>
        {
            if (token == generation && InteractionControl.IsUsable(main.modular)
                && !main.cardInspector.Visible)
            {
                main.modular.GrabFocus();
            }
        }).CallDeferred();
    }
}
