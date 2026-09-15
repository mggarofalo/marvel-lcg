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
            panel.CardPreview.Show(control, ids[0]);
        };
        control.MouseExited += () =>
        {
            pointerInside = false;
            if (!control.HasFocus()) panel.CardPreview.Dismiss(control);
        };
        control.GuiInput += input =>
        {
            if (input is InputEventMouseMotion)
            {
                pointerInside = true;
                panel.CardPreview.Show(control, ids[0]);
            }
        };
        control.FocusEntered += () =>
        {
            panel.NotifyAnchorFocused(ids);
            panel.CardPreview.Show(control, ids[0]);
        };
        control.FocusExited += () => { if (!pointerInside) panel.CardPreview.Dismiss(control); };
    }
}
