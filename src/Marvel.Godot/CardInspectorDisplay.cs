using Godot;

namespace Marvel.Godot;

/// <summary>Applies modal and focus behavior after inspector geometry is resolved.</summary>
internal static class CardInspectorDisplay
{
    internal static void Show(
        Main main, Control detail, Control? priorFocus, bool pinned, bool attached)
    {
        main.cardInspectorPinned = pinned;
        main.cardInspector.MouseFilter = pinned
            ? Control.MouseFilterEnum.Stop
            : Control.MouseFilterEnum.Ignore;
        // An attached popover keeps the source readable. Only the intentional
        // viewport fallback uses the dimmed modal backdrop.
        main.cardInspectorBackdrop.Visible = pinned && !attached;
        main.cardInspectorClose.Visible = false;
        main.cardInspector.Visible = true;
        if (pinned)
        {
            main.cardInspectorState.ReturnFocus = priorFocus;
            Callable.From(detail.GrabFocus).CallDeferred();
        }
        else if (priorFocus is not null
                 && GodotObject.IsInstanceValid(priorFocus)
                 && priorFocus.IsInsideTree()
                 && !priorFocus.IsQueuedForDeletion()
                 && !main.cardInspector.IsAncestorOf(priorFocus))
        {
            Callable.From(priorFocus.GrabFocus).CallDeferred();
        }
    }
}
