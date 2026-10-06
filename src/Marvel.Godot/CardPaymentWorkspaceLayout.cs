using Godot;

namespace Marvel.Godot;

/// <summary>Reserves a temporary sidebar workspace without covering the physical table.</summary>
internal static class CardPaymentWorkspaceLayout
{
    internal static Main MainFor(Node node)
    {
        for (Node? parent = node; parent is not null; parent = parent.GetParent())
            if (parent is Main main) return main;
        throw new InvalidOperationException("The payment workspace needs its table host.");
    }

    internal static bool Active(Main main) =>
        CardPaymentPresentation.UsesModal(main.decisions.composer, main.decisions.submitting);

    internal static float Width(Vector2 viewport) => Math.Min(560, Math.Max(280, viewport.X - 1360));

    internal static float Height(Vector2 viewport) => Math.Clamp(viewport.Y - 440, 360, 680);

    internal static void Install(Main main, PanelContainer frame)
    {
        VBoxContainer stack = main.promptStack;
        stack.AddChild(frame);
        stack.MoveChild(frame, stack.GetNode<Control>("Workbench").GetIndex());
        Fit(main, frame);
    }

    internal static void Refresh(Main main)
    {
        if (!InteractionControl.IsUsable(main)) return;
        main.ApplyResponsivePlayLayout();
        main.RefreshSynchronizeAvailability();
    }

    internal static void Fit(Main main, Control frame) =>
        frame.CustomMinimumSize = new Vector2(0, Height(main.GetViewportRect().Size));
}
