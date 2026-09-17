using Godot;

namespace Marvel.Godot;

/// <summary>Turns the former decision dock into an independent, collapsible history drawer.</summary>
internal static class TableHistoryDrawer
{
    private const string ExpandedMeta = "history_drawer_expanded";

    internal static bool IsExpanded(Main main) =>
        main.promptPanel.HasMeta(ExpandedMeta)
        && main.promptPanel.GetMeta(ExpandedMeta).AsBool();

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
            return;
        }

        TabContainer workbench = main.GetNode<TabContainer>(
            "Margin/Shell/Content/Play/Prompt/Margin/Stack/Workbench");
        Control history = workbench.GetNode<Control>("History");
        history.Visible = true;
        main.decisions.Visible = false;
        main.lastResult.Visible = false;
        main.activeResolution.Visible = false;
        workbench.TabsVisible = false;
        workbench.CurrentTab = history.GetIndex();
        main.promptPanel.Visible = true;
        main.GetNode<Control>(
            "Margin/Shell/Content/Play/Prompt/Margin/Stack/PromptHeader").Visible = false;
        main.GetNode<Control>(
            "Margin/Shell/Content/Play/Prompt/Margin/Stack/HeaderRule").Visible = false;

        Button toggle = EnsureToggle(main, history);
        Label latest = EnsureLatestResult(main, history);
        latest.AutowrapMode = TextServer.AutowrapMode.Off;
        latest.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        latest.CustomMinimumSize = new Vector2(0, 44);
        latest.TooltipText = latest.Text;
        Button dismiss = EnsureDismiss(main, history, latest);
        HFlowContainer actions = EnsureActionFlow(history, dismiss);
        bool expanded = IsExpanded(main);
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
        toggle.Text = expanded ? "Collapse history" : "History";
        history.GetNode<Control>("EventHeader/Heading").Visible = expanded;
        foreach (Control control in new Control[]
                 {
                     main.promptDiagnostic,
                     history.GetNode<Control>("Rule"),
                     main.eventCue,
                     latest,
                     main.eventLog,
                     dismiss,
                     actions,
                 })
        {
            control.Visible = expanded;
        }
        foreach (string name in new[] { "Skip", "UndoLast", "CopyReport", "SaveReport" })
        {
            if (history.FindChild(name, recursive: true, owned: false) is Control control)
            {
                control.Visible = expanded;
            }
        }
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

    private static Label EnsureLatestResult(Main main, Control history)
    {
        if (history.GetNodeOrNull<Label>("LatestResult") is { } existing)
        {
            return existing;
        }
        var latest = new Label
        {
            Name = "LatestResult",
            Text = main.lastResultSummary.Text,
            AutowrapMode = TextServer.AutowrapMode.Off,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            CustomMinimumSize = new Vector2(0, 44),
            ThemeTypeVariation = GodotThemeVariations.Body,
        };
        history.AddChild(latest);
        history.MoveChild(latest, Math.Max(0, main.eventLog.GetIndex()));
        return latest;
    }

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
