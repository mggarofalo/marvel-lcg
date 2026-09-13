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
        InterfaceScale workspaceScale = VisualSystem.TabletopScale(scale);
        main.board.Theme = ClientTheme.Create(workspaceScale);
        // The scale control is the ruler for the rest of the interface. Keep
        // its own geometry fixed so changing the value does not move the
        // pointer target beneath the user's hand.
        main.GetNode<Control>("StatusBar").Theme = ClientTheme.Create(InterfaceScale.Compact);
        main.interfaceScaleValue.Text = $"Scale {Mathf.RoundToInt(VisualSystem.ScalePercent(scale))}%";
        main.decisions.SetInterfaceScale(scale);
        DensityMetrics density = VisualSystem.Density(scale);
        DensityMetrics toolbarDensity = VisualSystem.Density(InterfaceScale.Compact);
        float minimumHeight = VisualSystem.Controls(scale).MinimumHeight;
        Control page = main.pageScroll;
        page.OffsetLeft = density.ViewportInset;
        page.OffsetTop = density.ViewportInset;
        page.OffsetRight = -density.ViewportInset;
        page.OffsetBottom = -density.ViewportInset;
        Control statusBar = main.GetNode<Control>("StatusBar");
        statusBar.OffsetLeft = toolbarDensity.ViewportInset;
        statusBar.OffsetTop = toolbarDensity.ViewportInset;
        statusBar.OffsetRight = -toolbarDensity.ViewportInset;
        statusBar.OffsetBottom = toolbarDensity.ViewportInset
            + VisualSystem.Controls(InterfaceScale.Compact).MinimumHeight;
        main.GetNode<Control>("Margin/Shell/Content/StatusBarClearance").CustomMinimumSize =
            new Vector2(0, VisualSystem.Controls(InterfaceScale.Standard).MinimumHeight);
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
        main.eventMotion.CustomMinimumSize = new Vector2(
            main.eventMotion.CustomMinimumSize.X,
            VisualSystem.Controls(InterfaceScale.Standard).MinimumHeight);
        main.eventSkip.CustomMinimumSize = new Vector2(
            main.eventSkip.CustomMinimumSize.X,
            VisualSystem.Controls(workspaceScale).MinimumHeight);
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
        main.title.Visible = true;
        main.eyebrow.Visible = true;
        main.description.Text = joinMode
            ? "Connect to an already-running engine and use a one-time seat invitation."
            : "Choose an authored Core Set assignment. The engine validates it again when play starts.";
        main.RefreshEntryAvailability();
    }

    internal void ApplyResponsivePlayLayout()
    {
        // This is a presentation choice: the scenario, selected player, hand,
        // and decision dock share one fixed 1920x1080 desktop canvas.
        DesktopPlayMetrics layout = VisualSystem.DesktopPlay(
            Math.Max(1, Mathf.RoundToInt(main.Size.X)),
            Math.Max(1, Mathf.RoundToInt(main.Size.Y)),
            VisualSystem.TabletopScale(main.interfaceScale));
        main.promptPanel.CustomMinimumSize = new Vector2(0, layout.DecisionDockHeight);
        main.setupGrid.Columns = 4;
        main.contentStack.ThemeTypeVariation = main.board.Visible
            ? GodotThemeVariations.TightStack
            : GodotThemeVariations.Stack;
        main.promptStack.ThemeTypeVariation = GodotThemeVariations.TightStack;
        main.decisions.CustomMinimumSize = new Vector2(
            0, Math.Max(80, layout.DecisionDockHeight / 2));
        main.eventCue.CustomMinimumSize = new Vector2(0, 52);
        main.eventLog.CustomMinimumSize = new Vector2(0, 140);
        main.pageScroll.HorizontalScrollMode = main.board.Visible
            ? ScrollContainer.ScrollMode.Disabled
            : ScrollContainer.ScrollMode.Auto;
        main.pageScroll.FollowFocus = !main.board.Visible;
        main.pageScroll.VerticalScrollMode = main.board.Visible
            ? ScrollContainer.ScrollMode.Disabled
            : ScrollContainer.ScrollMode.Auto;
    }
}
