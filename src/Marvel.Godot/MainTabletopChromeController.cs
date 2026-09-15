using Godot;

namespace Marvel.Godot;

/// <summary>Owns compact tabletop chrome and mulligan-specific visibility.</summary>
internal sealed class MainTabletopChromeController
{
    private readonly Main main;

    internal MainTabletopChromeController(Main main)
    {
        this.main = main;
    }

    internal void Configure(bool compactHeight, bool compactTableChrome)
    {
        main.setupGrid.Columns = main.GetViewportRect().Size.X >= 1500 ? 4 : 2;
        main.contentStack.ThemeTypeVariation = main.board.Visible
            && (compactHeight || compactTableChrome)
            ? GodotThemeVariations.TightStack
            : GodotThemeVariations.Stack;
        main.promptStack.ThemeTypeVariation = compactHeight
            ? GodotThemeVariations.TightStack
            : GodotThemeVariations.Stack;
        SetTableChrome(compactTableChrome);
        main.eventCue.CustomMinimumSize = new Vector2(0, 68);
        main.eventLog.CustomMinimumSize = new Vector2(0, compactHeight ? 180 : 300);
    }

    internal void SetMulligan(bool mulligan)
    {
        foreach (string path in new[]
                 {
                     "Margin/Shell/Content/Play/Prompt/Margin/Stack/PromptHeader",
                     "Margin/Shell/Content/Play/Prompt/Margin/Stack/HeaderRule",
                     "Margin/Shell/Content/Play/Prompt/Margin/Stack/Workbench/History",
                 })
        {
            main.GetNode<Control>(path).Visible = !mulligan;
        }

        main.activeResolution.Visible = !mulligan
            && !string.IsNullOrWhiteSpace(main.activeResolutionSummary.Text);

        TabContainer workbench = main.GetNode<TabContainer>(
            "Margin/Shell/Content/Play/Prompt/Margin/Stack/Workbench");
        workbench.TabsVisible = !mulligan;
        workbench.CurrentTab = 0;
    }

    private void SetTableChrome(bool compact)
    {
        main.GetNode<Control>("Margin/Shell/Content/StatusBarClearance").CustomMinimumSize =
            new Vector2(0, compact ? 32 : 38);
        foreach (string path in new[]
                 {
                     "Margin/Shell/Content/Play/Prompt/Margin/Stack/PromptHeader/Eyebrow",
                     "Margin/Shell/Content/Play/Prompt/Margin/Stack/PromptHeader/Context",
                     "Margin/Shell/Content/Play/Prompt/Margin/Stack/PromptHeader/Requirement",
                 })
        {
            main.GetNode<Control>(path).Visible = !compact;
        }
        main.eyebrow.Visible = !compact;
        main.title.Visible = !compact;
        main.description.Visible = !compact;
        main.statusPanel.Visible = !compact;
        main.GetNode<Control>("Margin/Shell").Theme = compact
            ? ClientTheme.Create(InterfaceScale.Standard)
            : null;
        main.board.Theme = compact ? ClientTheme.Create(InterfaceScale.Standard) : null;
        main.promptPanel.Theme = compact ? ClientTheme.Create(InterfaceScale.Standard) : null;
        main.promptPanel.ThemeTypeVariation = compact
            ? GodotThemeVariations.TabletopDock
            : GodotThemeVariations.SurfacePanel;
        if (compact)
        {
            main.boardAreas.AddThemeConstantOverride("separation", 0);
            main.GetNode<MarginContainer>("Margin/Shell/Content/Play/Board/TableScroll/Margin")
                .AddThemeConstantOverride("margin_bottom", 0);
            main.GetNode<MarginContainer>("Margin/Shell/Content/Play/Board/HandShelf/Margin")
                .AddThemeConstantOverride("margin_top", 0);
            main.GetNode<MarginContainer>("Margin/Shell/Content/Play/Board/HandShelf/Margin")
                .AddThemeConstantOverride("margin_bottom", 0);
        }
        else
        {
            main.boardAreas.RemoveThemeConstantOverride("separation");
            main.GetNode<MarginContainer>("Margin/Shell/Content/Play/Board/TableScroll/Margin")
                .RemoveThemeConstantOverride("margin_bottom");
            main.GetNode<MarginContainer>("Margin/Shell/Content/Play/Board/HandShelf/Margin")
                .RemoveThemeConstantOverride("margin_top");
            main.GetNode<MarginContainer>("Margin/Shell/Content/Play/Board/HandShelf/Margin")
                .RemoveThemeConstantOverride("margin_bottom");
        }
        main.GetNode<PanelContainer>("Margin/Shell/Content/Play/Board/HandShelf").ThemeTypeVariation = compact
            ? GodotThemeVariations.TabletopShelf
            : GodotThemeVariations.SurfacePanel;
    }
}
