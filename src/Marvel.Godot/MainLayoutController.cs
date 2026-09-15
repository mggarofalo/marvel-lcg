using Godot;

namespace Marvel.Godot;

/// <summary>Owns responsive play layout and presentation scale changes.</summary>
internal sealed class MainLayoutController
{
    private readonly Main main;
    private readonly MainTabletopChromeController tabletopChrome;

    internal MainLayoutController(Main main)
    {
        this.main = main;
        tabletopChrome = new MainTabletopChromeController(main);
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
            main.RenderPromptSummary(main.CurrentGame.Prompt, world);
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
        // This is a presentation choice. The desktop table owns the canvas,
        // with a stable decision dock beside the flexible table column. Only
        // bounded, task-local surfaces such as the hand and decision body may
        // scroll; the page can never move Commit away.
        Vector2 viewport = main.GetViewportRect().Size;
        DesktopPlayMetrics layout = VisualSystem.DesktopPlay(
            Math.Max(1, Mathf.RoundToInt(viewport.X)),
            Math.Max(1, Mathf.RoundToInt(viewport.Y)),
            main.interfaceScale);
        bool compactHeight = viewport.Y < 800;
        bool gameplay = main.board.Visible;
        bool mulligan = MulliganPrompt.IsOpening(main.CurrentGame?.Prompt);
        bool fixedTabletop = gameplay && DesktopTabletop.Uses(viewport);
        bool compactTableChrome = fixedTabletop;
        main.playLayout.Columns = fixedTabletop ? 2 : 1;
        main.playLayout.ThemeTypeVariation = GodotThemeVariations.PlayGrid;
        main.promptPanel.SizeFlagsHorizontal = fixedTabletop
            ? Control.SizeFlags.Fill
            : Control.SizeFlags.ExpandFill;
        main.promptPanel.SizeFlagsVertical = fixedTabletop
            ? Control.SizeFlags.ExpandFill
            : Control.SizeFlags.Fill;
        main.GetNode<ScrollContainer>(
            "Margin/Shell/Content/Play/Board/TableScroll").CustomMinimumSize = new Vector2(
                0,
                fixedTabletop
                    ? Math.Min(
                        VisualSystem.Card(CardDisplaySize.Board, main.interfaceScale).MinimumHeight,
                        VisualSystem.Controls(main.interfaceScale).MinimumPointerTarget * 2
                        + VisualSystem.Spacing(main.interfaceScale).Small)
                    : 96);
        main.decisions.SetCompactMulliganChrome(compactTableChrome);
        ConfigureDecisionDock(mulligan && compactTableChrome, compactTableChrome, layout);
        tabletopChrome.Configure(compactHeight, compactTableChrome);
        ConfigurePlayScrolling(gameplay, fixedTabletop, mulligan);
        main.boardController.RerenderForViewport(viewport);
    }

    private void ConfigureDecisionDock(
        bool mulligan,
        bool compactTableChrome,
        DesktopPlayMetrics layout)
    {
        InterfaceScale dockScale = compactTableChrome ? InterfaceScale.Standard : main.interfaceScale;
        float decisionHeight = mulligan
            ? Math.Max(172, VisualSystem.Controls(dockScale).MinimumPointerTarget * 3 + 16)
            : compactTableChrome ? 220 : layout.DecisionMinimumHeight;
        main.promptPanel.CustomMinimumSize = new Vector2(
            compactTableChrome ? layout.DecisionWidth : 0,
            decisionHeight);
        main.decisions.CustomMinimumSize = new Vector2(
            0, mulligan ? 172 : decisionHeight);
        tabletopChrome.SetMulligan(mulligan);
        main.decisions.ResetSize();
        main.promptPanel.ResetSize();
        main.playLayout.ResetSize();
        main.contentStack.ResetSize();
        main.GetNode<PanelContainer>("Margin/Shell").ResetSize();
        main.playLayout.QueueSort();
        main.contentStack.QueueSort();
    }

    private void ConfigurePlayScrolling(bool gameplay, bool fixedTabletop, bool mulligan)
    {
        Vector2 viewport = main.GetViewportRect().Size;
        bool desktopGameplay = gameplay && DesktopTabletop.Uses(viewport);
        ConfigurePageScrolling(gameplay, desktopGameplay);
        ConfigureTableScrolling(fixedTabletop);
    }

    private void ConfigurePageScrolling(bool gameplay, bool desktopGameplay)
    {
        PanelContainer shell = main.GetNode<PanelContainer>("Margin/Shell");
        main.pageScroll.OffsetTop = desktopGameplay ? 4 : 16;
        main.pageScroll.OffsetBottom = desktopGameplay ? 0 : -16;
        shell.CustomMinimumSize = new Vector2(
            desktopGameplay
                ? Math.Max(0, main.GetViewportRect().Size.X - 32)
                : 0,
            shell.CustomMinimumSize.Y);
        main.pageScroll.HorizontalScrollMode = gameplay
            ? ScrollContainer.ScrollMode.Disabled
            : ScrollContainer.ScrollMode.Auto;
        main.pageScroll.FollowFocus = !gameplay || main.invitationOffer.Visible;
        main.pageScroll.VerticalScrollMode = desktopGameplay
            ? ScrollContainer.ScrollMode.Disabled
            : gameplay
                ? ScrollContainer.ScrollMode.Auto
                : PlayScrollingPolicy.PageVerticalScrollMode(
                    false, main.invitationOffer.Visible, main.interfaceScale);
        if (desktopGameplay)
        {
            main.pageScroll.ScrollVertical = 0;
            main.playLayout.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        }
        else
        {
            main.playLayout.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        }
    }

    private void ConfigureTableScrolling(bool fixedTabletop)
    {
        ScrollContainer table = main.GetNode<ScrollContainer>(
            "Margin/Shell/Content/Play/Board/TableScroll");
        table.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        table.VerticalScrollMode = fixedTabletop
            ? ScrollContainer.ScrollMode.Disabled
            : ScrollContainer.ScrollMode.Auto;
        if (main.handRail.GetParent() is ScrollContainer hand)
        {
            hand.HorizontalScrollMode = fixedTabletop
                ? ScrollContainer.ScrollMode.ShowNever
                : ScrollContainer.ScrollMode.Auto;
            if (fixedTabletop)
            {
                hand.ScrollHorizontal = 0;
            }
        }
        if (table.HorizontalScrollMode == ScrollContainer.ScrollMode.Disabled)
        {
            table.ScrollHorizontal = 0;
        }
        if (table.VerticalScrollMode == ScrollContainer.ScrollMode.Disabled)
        {
            table.ScrollVertical = 0;
        }
    }

}
