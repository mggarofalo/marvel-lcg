using Godot;

namespace Marvel.Godot;

/// <summary>Reveals the active draft when a direct table gesture starts composing it.</summary>
internal static class DraftWorkspaceFocus
{
    internal static void Show(Main main)
    {
        ArgumentNullException.ThrowIfNull(main);
        if (DesktopTabletop.Uses(main.GetViewportRect().Size)) return;
        main.GetNode<TabContainer>(
            "Margin/Shell/Content/Play/Prompt/Margin/Stack/Workbench").CurrentTab = 0;
    }
}
