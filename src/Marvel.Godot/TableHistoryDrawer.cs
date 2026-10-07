using Godot;

namespace Marvel.Godot;

/// <summary>Turns the former decision dock into an independent, collapsible history drawer.</summary>
internal static class TableHistoryDrawer
{
    private const string ExpandedMeta = "history_drawer_expanded";

    internal static bool IsExpanded(Main main) =>
        main.promptPanel.HasMeta(ExpandedMeta)
        && main.promptPanel.GetMeta(ExpandedMeta).AsBool();

    internal static void SelectHistory(Main main)
    {
        TabContainer workbench = main.GetNode<TabContainer>(
            "Margin/Shell/Content/Play/Prompt/Margin/Stack/Workbench");
        workbench.CurrentTab = workbench.GetNode<Control>("History").GetIndex();
    }

    internal static float ExpandedWidth(Main main)
    {
        Vector2 viewport = main.GetViewportRect().Size;
        DesktopPlayMetrics preferred = VisualSystem.DesktopPlay(
            Math.Max(1, Mathf.RoundToInt(viewport.X)),
            Math.Max(1, Mathf.RoundToInt(viewport.Y)),
            main.interfaceScale);
        return Math.Min(preferred.DecisionWidth, Math.Max(300, viewport.X - 960));
    }

    internal static void Configure(Main main, bool desktop)
    {
        if (!desktop)
        {
            main.promptPanel.Visible = true;
            main.decisions.Visible = true;
            HistoryUndoControl.RefreshReason(main, true);
            return;
        }

        TabContainer workbench = main.GetNode<TabContainer>(
            "Margin/Shell/Content/Play/Prompt/Margin/Stack/Workbench");
        Control history = workbench.GetNode<Control>("History");
        history.Visible = true;
        main.decisions.Visible = main.decisions.CompleteChoicesOpen;
        main.lastResult.Visible = false;
        main.activeResolution.Visible = false;
        workbench.TabsVisible = false;
        SelectHistory(main);
        main.promptPanel.Visible = true;
        main.GetNode<Control>(
            "Margin/Shell/Content/Play/Prompt/Margin/Stack/PromptHeader").Visible = false;
        main.GetNode<Control>(
            "Margin/Shell/Content/Play/Prompt/Margin/Stack/HeaderRule").Visible = false;

        Button toggle = EnsureToggle(main, history);
        Label latest = EnsureLatestResult(main, history);
        latest.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        latest.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        latest.CustomMinimumSize = Vector2.Zero;
        latest.TooltipText = latest.Text;
        Button dismiss = EnsureDismiss(main, history, latest);
        HFlowContainer actions = EnsureActionFlow(history, dismiss);
        bool expanded = IsExpanded(main);
        HistoryUndoControl.RefreshReason(main, expanded);
        bool payment = CardPaymentWorkspaceLayout.Active(main);
        if (history.GetNodeOrNull<Control>("TableSidebar") is { } sidebar) sidebar.Visible = !payment;
        TableHistoryViewport.Configure(main, payment);
        ConfigureCueSummary(main, expanded);
        toggle.Text = expanded ? "Collapse history" : "History";
        history.GetNode<Control>("EventHeader/Heading").Visible = false;
        foreach (Control control in new Control[]
                 {
                     history.GetNode<Control>("Rule"),
                     main.eventLog,
                     dismiss,
                     actions,
                 })
        {
            control.Visible = expanded;
        }
        TableLatestResult.Configure(latest, expanded);
        if (payment) ((Control)latest.GetParent().GetParent()).Visible = false;
        main.eventController.RefreshEventCueVisibility();
        main.promptDiagnostic.Visible = false;
        if (history.FindChild("Skip", recursive: true, owned: false) is Button skip)
            skip.Text = "Finish animation";
        foreach (string name in new[] { "Skip", "UndoLast", "CopyReport", "SaveReport" })
        {
            if (history.FindChild(name, recursive: true, owned: false) is Control control)
            {
                control.Visible = expanded;
            }
        }
    }

    private static void ConfigureCueSummary(Main main, bool expanded)
    {
        main.eventCueSummary.AutowrapMode = expanded
            ? TextServer.AutowrapMode.WordSmart
            : TextServer.AutowrapMode.Off;
        main.eventCueSummary.TextOverrunBehavior = expanded
            ? TextServer.OverrunBehavior.NoTrimming
            : TextServer.OverrunBehavior.TrimEllipsis;
        main.eventCueSummary.CustomMinimumSize = expanded
            ? Vector2.Zero
            : new Vector2(0, 44);
        main.eventCueSummary.TooltipText = main.eventCueSummary.Text;
    }

    private static HFlowContainer EnsureActionFlow(Control history, Button dismiss)
    {
        HFlowContainer actions = history.GetNodeOrNull<HFlowContainer>("HistoryActions")
            ?? new HFlowContainer
            {
                Name = "HistoryActions",
                ThemeTypeVariation = GodotThemeVariations.CompactRow,
            };
        if (actions.GetParent() is null)
        {
            history.AddChild(actions);
            history.MoveChild(actions, 1);
        }
        foreach (string name in new[] { "Skip", "UndoLast", "CopyReport", "SaveReport" })
        {
            if (history.FindChild(name, recursive: true, owned: false) is Control control
                && control.GetParent() != actions)
            {
                control.Reparent(actions);
            }
        }
        if (dismiss.GetParent() != actions)
        {
            dismiss.Reparent(actions);
        }
        return actions;
    }

    private static Button EnsureDismiss(Main main, Control history, Label latest)
    {
        Control header = history.GetNode<Control>("EventHeader");
        if (history.FindChild("DismissHistoryResult", recursive: true, owned: false)
            is Button existing)
        {
            return existing;
        }
        var dismiss = new Button
        {
            Name = "DismissHistoryResult",
            Text = "Dismiss result",
            TooltipText = "Dismiss the latest result without clearing game history.",
            CustomMinimumSize = new Vector2(140, 44),
        };
        dismiss.Pressed += () =>
        {
            latest.Text = string.Empty;
            main.DismissLastResult();
        };
        header.AddChild(dismiss);
        header.MoveChild(dismiss, 1);
        return dismiss;
    }

    internal static Label EnsureLatestResult(Main main, Control history) => TableLatestResult.Ensure(main, history);

    private static Button EnsureToggle(Main main, Control history)
    {
        Control header = history.GetNode<Control>("EventHeader");
        if (header.GetNodeOrNull<Button>("ToggleHistory") is { } existing)
        {
            return existing;
        }
        var toggle = new Button
        {
            Name = "ToggleHistory",
            Text = "History",
            TooltipText = "Expand or collapse the game history without covering the table.",
            CustomMinimumSize = new Vector2(140, 44),
        };
        toggle.Pressed += () =>
        {
            main.promptPanel.SetMeta(ExpandedMeta, !IsExpanded(main));
            Callable.From(main.layoutController.ApplyResponsivePlayLayout).CallDeferred();
        };
        header.AddChild(toggle);
        header.MoveChild(toggle, 0);
        return toggle;
    }
}
