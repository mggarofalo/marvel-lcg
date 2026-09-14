using Godot;

namespace Marvel.Godot;

/// <summary>Owns responsive play layout and presentation scale changes.</summary>
internal sealed class MainLayoutController
{
    private readonly Main main;

    internal MainLayoutController(Main main)
    {
        this.main = main;
    }
    internal void ApplyInterfaceScale(InterfaceScale scale)
    {
        main.interfaceScale = scale;
        main.Theme = ClientTheme.Create(scale);
        // The scale control is the ruler for the rest of the interface. Keep
        // its own geometry fixed so changing the value does not move the
        // pointer target beneath the user's hand.
        main.GetNode<Control>("StatusBar").Theme = ClientTheme.Create(InterfaceScale.Compact);
        main.interfaceScaleValue.Text = $"Scale {Mathf.RoundToInt(VisualSystem.ScalePercent(scale))}%";
        main.decisions.SetInterfaceScale(scale);
        float minimumHeight = VisualSystem.Controls(scale).MinimumHeight;
        foreach (Control control in new Control[]
                 {
                     main.endpoint, main.gameId, main.hero, main.secondHero, main.scenario, main.mode, main.modular, main.seed,
                     main.invitation,
                 })
        {
            control.CustomMinimumSize = new Vector2(
                control.CustomMinimumSize.X,
                minimumHeight);
        }

        foreach (Control control in new Control[]
                 {
                     main.startFlow, main.joinFlow, main.reloadSetup, main.start, main.join, main.invitationCopy,
                     main.lastResultToggle, main.lastResultDismiss, main.undoLast,
                 })
        {
            control.CustomMinimumSize = new Vector2(
                control.CustomMinimumSize.X,
                minimumHeight);
        }
        main.eventSkip.CustomMinimumSize = new Vector2(
            main.eventSkip.CustomMinimumSize.X,
            minimumHeight);
        if (main.CurrentGame?.World is { } world)
        {
            main.RenderBoard(world);
        }
        ApplyResponsivePlayLayout();
    }

    internal void ShowEntryMode(bool joinMode)
    {
        main.joining = joinMode;
        main.setupHeading.Visible = !joinMode;
        main.reloadSetup.Visible = !joinMode;
        main.setupGrid.Visible = !joinMode;
        main.seedHelp.Visible = !joinMode;
        main.start.Visible = !joinMode;
        main.joinFields.Visible = joinMode;
        main.GetNode<Control>("Margin/Shell/Content/Setup/Briefing").Visible = !joinMode;
        main.startFlow.Disabled = false;
        main.joinFlow.Disabled = false;
        main.startFlow.ThemeTypeVariation = joinMode
            ? string.Empty
            : GodotThemeVariations.PrimaryButton;
        main.joinFlow.ThemeTypeVariation = joinMode
            ? GodotThemeVariations.PrimaryButton
            : string.Empty;
        main.eyebrow.Text = joinMode
            ? "CORE SET  /  JOIN TABLE"
            : "CORE SET  /  MISSION BRIEFING";
        main.title.ThemeTypeVariation = GodotThemeVariations.DisplayTitle;
        main.description.Visible = true;
        main.description.Text = joinMode
            ? "Connect to an already-running engine and use a one-time seat invitation."
            : "Choose an authored Core Set assignment. The engine validates it again when play starts.";
        main.RefreshEntryAvailability();
    }

    internal void ApplyResponsivePlayLayout()
    {
        // This is a presentation choice. Gameplay is a full-width table whose
        // decision dock remains below the hand; neither the page nor the table
        // can scroll the current decision away.
        DesktopPlayMetrics layout = VisualSystem.DesktopPlay(
            Math.Max(1, Mathf.RoundToInt(main.Size.X)),
            Math.Max(1, Mathf.RoundToInt(main.Size.Y)),
            main.interfaceScale);
        bool compactHeight = main.Size.Y < 800;
        bool gameplay = main.board.Visible;
        bool mulligan = MulliganPrompt.IsOpening(main.CurrentGame?.Prompt);
        bool fixedTabletop = gameplay && mulligan && main.Size.X >= 1800 && main.Size.Y >= 900;
        bool compactTableChrome = fixedTabletop && mulligan;
        main.decisions.SetCompactMulliganChrome(compactTableChrome);
        ConfigureDecisionDock(mulligan && compactTableChrome, compactTableChrome, layout);
        ConfigureStackChrome(compactHeight, compactTableChrome);
        ConfigurePlayScrolling(gameplay, fixedTabletop);
    }

    private void ConfigureDecisionDock(
        bool mulligan,
        bool compactTableChrome,
        DesktopPlayMetrics layout)
    {
        InterfaceScale dockScale = compactTableChrome ? InterfaceScale.Standard : main.interfaceScale;
        main.promptPanel.CustomMinimumSize = new Vector2(
            0,
            mulligan
                ? Math.Max(172, VisualSystem.Controls(dockScale).MinimumPointerTarget * 3 + 16)
                : layout.DecisionMinimumHeight);
        main.decisions.CustomMinimumSize = new Vector2(
            0, mulligan ? 172 : layout.DecisionMinimumHeight);
        SetMulliganDockChrome(mulligan);
    }

    private void ConfigureStackChrome(bool compactHeight, bool compactTableChrome)
    {
        main.setupGrid.Columns = main.Size.X >= 1500 ? 4 : 2;
        main.contentStack.ThemeTypeVariation = main.board.Visible && compactHeight
            ? GodotThemeVariations.TightStack
            : GodotThemeVariations.Stack;
        main.promptStack.ThemeTypeVariation = compactHeight
            ? GodotThemeVariations.TightStack
            : GodotThemeVariations.Stack;
        SetTableChrome(compactTableChrome);
        main.eventCue.CustomMinimumSize = new Vector2(0, 68);
        main.eventLog.CustomMinimumSize = new Vector2(0, compactHeight ? 180 : 300);
    }

    private void ConfigurePlayScrolling(bool gameplay, bool fixedTabletop)
    {
        main.pageScroll.HorizontalScrollMode = gameplay
            ? ScrollContainer.ScrollMode.Disabled
            : ScrollContainer.ScrollMode.Auto;
        main.pageScroll.FollowFocus = !gameplay || main.invitationOffer.Visible;
        main.pageScroll.VerticalScrollMode = fixedTabletop
            ? ScrollContainer.ScrollMode.Disabled
            : gameplay
                ? ScrollContainer.ScrollMode.Auto
                : PageVerticalScrollMode(false, main.invitationOffer.Visible, main.interfaceScale);
        main.GetNode<ScrollContainer>(
            "Margin/Shell/Content/Play/Board/TableScroll").VerticalScrollMode = fixedTabletop
                ? ScrollContainer.ScrollMode.Disabled
                : ScrollContainer.ScrollMode.Auto;
    }

    internal static ScrollContainer.ScrollMode PageVerticalScrollMode(
        bool boardVisible,
        bool invitationVisible,
        InterfaceScale scale) => !boardVisible || invitationVisible || (int)scale > 100
            ? ScrollContainer.ScrollMode.Auto
            : ScrollContainer.ScrollMode.Disabled;

    private void SetMulliganDockChrome(bool mulligan)
    {
        foreach (string path in new[]
                 {
                     "Margin/Shell/Content/Play/Prompt/Margin/Stack/PromptHeader",
                     "Margin/Shell/Content/Play/Prompt/Margin/Stack/ActiveResolution",
                     "Margin/Shell/Content/Play/Prompt/Margin/Stack/HeaderRule",
                     "Margin/Shell/Content/Play/Prompt/Margin/Stack/Workbench/History",
                 })
        {
            main.GetNode<Control>(path).Visible = !mulligan;
        }

        TabContainer workbench = main.GetNode<TabContainer>(
            "Margin/Shell/Content/Play/Prompt/Margin/Stack/Workbench");
        workbench.TabsVisible = !mulligan;
        workbench.CurrentTab = 0;
    }

    private void SetTableChrome(bool compact)
    {
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
                .AddThemeConstantOverride("margin_top", 4);
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
