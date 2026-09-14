using Godot;

namespace Marvel.Godot;

/// <summary>Binds decision controls to visible board anchors without pointer-driven scrolling.</summary>
internal static class DecisionAnchorBinding
{
    internal static void Bind(DecisionPanel panel, Control control, IReadOnlyList<int> ids)
    {
        if (ids.Count == 0) return;
        bool pointerInside = false;
        control.MouseEntered += () =>
        {
            pointerInside = true;
            panel.NotifyCardHovered(ids[0]);
        };
        control.MouseExited += () =>
        {
            pointerInside = false;
            if (!control.HasFocus()) panel.NotifyCardHovered(null);
        };
        control.FocusEntered += () => panel.NotifyAnchorFocused(ids);
        control.FocusExited += () => { if (!pointerInside) panel.NotifyCardHovered(null); };
    }
}
