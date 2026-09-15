using Godot;
using Marvel.Client;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns prompt summaries, event chronology, and progress presentation.</summary>
internal sealed class MainEventController
{
    private readonly Main main;
    private readonly MainEventMotionController motion;

    internal MainEventController(Main main)
    {
        this.main = main;
        motion = new MainEventMotionController(main);
    }
    internal void RevealOutcome()
    {
        main.GetNode<TabContainer>(
            "Margin/Shell/Content/Play/Prompt/Margin/Stack/Workbench").CurrentTab = 0;
        main.GetViewport().GuiReleaseFocus();
        main.pageScroll.FollowFocus = false;
        main.pageScroll.ScrollVertical = 0;
        main.pageScroll.SetDeferred("scroll_vertical", 0);
    }

    internal void RenderPromptSummary(Prompt? prompt, WorldDescriptor world)
    {
        if (prompt is null)
        {
            (main.activeResolution.Visible, main.activeResolutionSummary.Text) = (false, string.Empty);
            (main.promptEyebrow.Text, main.promptHeading.Text, main.promptContext.Text) =
                world.Outcome switch
                {
                    Outcome.Unfinished => (
                        "OTHER PLAYER'S DECISION",
                        "Waiting for another player.",
                        "THE GAME IS STILL IN PROGRESS"),
                    Outcome.PlayersWin => (
                        "VICTORY",
                        "The players won.",
                        "THE FINAL VILLAIN STAGE WAS DEFEATED"),
                    Outcome.VillainWins => (
                        "DEFEAT",
                        "The villain won.",
                        "THE FINAL MAIN SCHEME WAS COMPLETED"),
                    Outcome.PlayersLose => (
                        "DEFEAT",
                        "The players lost.",
                        "THE ENCOUNTER COULD NOT CONTINUE"),
                    _ => ("GAME COMPLETE", "The game ended.", "THE TABLE IS SETTLED"),
                };
            main.promptRequirement.Text = world.Outcome == Outcome.Unfinished
                ? "WAITING"
                : "RESOLVED";
            main.promptRequirement.ThemeTypeVariation = GodotThemeVariations.StatusText;
            main.promptProgress.Text = "NO INPUT PENDING";
            main.promptDiagnostic.Text = "No prompt is pending.";
            if (world.Outcome != Outcome.Unfinished)
            {
                main.title.Text = world.Outcome == Outcome.PlayersWin ? "Victory" : "Defeat";
                main.description.Text = main.promptContext.Text;
            }
            return;
        }

        PromptPresentation view = PromptPresentation.From(prompt, world);
        main.promptEyebrow.Text = "CURRENT DECISION";
        main.promptHeading.Text = view.Heading;
        main.promptContext.Text = view.Context;
        main.activeResolution.Visible = !string.IsNullOrWhiteSpace(view.Resolution);
        main.activeResolutionSummary.Text = view.Resolution;
        main.promptRequirement.Text = view.Requirement;
        main.promptDiagnostic.Text = view.Diagnostic;
        main.promptRequirement.ThemeTypeVariation = prompt.Cancellable
            ? GodotThemeVariations.Caption
            : GodotThemeVariations.DangerText;
    }

    internal void RenderLastResult(
        IReadOnlyList<EventPresentation> highlights, bool reset)
    {
        if (highlights.Count == 0)
        {
            if (reset)
            {
                DismissLastResult();
            }
            return;
        }

        int generation = ++main.lastResultGeneration;
        main.lastResult.Visible = true;
        main.lastResultSummary.Text = string.Join(" ", highlights.Select(entry => entry.Summary));
        main.lastResult.ThemeTypeVariation = highlights.Any(entry => entry.Motion is
            EventMotionKind.Defeat or EventMotionKind.Terminal)
                ? GodotThemeVariations.DangerStatusPanel
                : GodotThemeVariations.StatusPanel;
        SetLastResultExpanded(true);
        main.GetTree().CreateTimer(Main.LastResultLifetimeSeconds).Timeout += () =>
        {
            if (generation == main.lastResultGeneration && main.IsInsideTree())
            {
                DismissLastResult();
            }
        };
    }

    internal void ToggleLastResult()
    {
        if (main.lastResult.Visible)
        {
            SetLastResultExpanded(!main.lastResultExpanded);
        }
    }

    internal void SetLastResultExpanded(bool expanded)
    {
        main.lastResultExpanded = expanded;
        main.lastResultSummary.Visible = expanded;
        main.lastResultToggle.Text = expanded ? "Collapse" : "Expand";
    }

    internal void DismissLastResult()
    {
        main.lastResultGeneration++;
        main.lastResultExpanded = false;
        main.lastResult.Visible = false;
        main.lastResultSummary.Visible = false;
        main.lastResultSummary.Text = string.Empty;
        main.lastResultToggle.Text = "Expand";
    }

    internal void RenderDecisionProgress(DecisionProgressPresentation? progress)
    {
        if (progress is null)
        {
            main.promptProgress.Text = "NO INPUT PENDING";
            main.promptProgress.ThemeTypeVariation = GodotThemeVariations.StatusText;
            return;
        }

        string targets = progress.Targets.Mode switch
        {
            TargetSelectionMode.None => "NO TARGETS",
            TargetSelectionMode.Grouped => $"GROUP {progress.Targets.Selected}/1",
            _ => progress.Targets.Minimum == progress.Targets.Maximum
                ? $"TARGETS {progress.Targets.Selected}/{progress.Targets.Minimum}"
                : $"TARGETS {progress.Targets.Selected} · NEED {progress.Targets.Minimum}–{progress.Targets.Maximum}",
        };
        string payment = progress.Payment.CostState switch
        {
            CostSelectionState.Unavailable => "CHOOSE AN ACTION",
            CostSelectionState.NotRequired => "FREE",
            CostSelectionState.Required => $"CHOOSE 1 OF {progress.Payment.CostOptions} COSTS",
            _ => $"PAYMENT {progress.Payment.AssignedIcons}/{progress.Payment.GeneratedIcons} ICONS"
                + (progress.Payment.RequestedVariables > 0
                    ? $" · VALUES {progress.Payment.DefinedVariables}/{progress.Payment.RequestedVariables}"
                    : string.Empty),
        };
        main.promptProgress.Text = $"{targets}  ·  {payment}  ·  "
            + (progress.IsReady ? "READY" : "INCOMPLETE");
        main.promptProgress.ThemeTypeVariation = progress.IsReady
            ? GodotThemeVariations.StatusText
            : GodotThemeVariations.Caption;
    }

    internal void RenderEvents()
    {
        IReadOnlyList<HistoryEntryDescriptor> actions =
            main.CurrentGame?.History?.Entries ?? [];
        if (actions.Count > 0 || main.CurrentGame?.History?.ActionOpen == true)
        {
            RenderActionHistory(actions);
            return;
        }
        if (main.events.Entries.Count == 0)
        {
            main.eventLog.Text = "No events yet.";
            return;
        }
        RenderEventChronology();
    }

    private void RenderActionHistory(IReadOnlyList<HistoryEntryDescriptor> actions)
    {
        string accent = ClientTheme.ToGodot(VisualSystem.Palette.Accent).ToHtml(false);
        IReadOnlyList<int> undo = main.CurrentGame!.History!.Undo;
        main.eventLog.Text = EventLogFormatter.FormatActions(actions, undo, accent);
        main.eventLog.ScrollToLine(main.eventLog.GetLineCount());
    }

    private void RenderEventChronology()
    {
        string accent = ClientTheme.ToGodot(VisualSystem.Palette.Accent).ToHtml(false);
        main.eventLog.Text = EventLogFormatter.FormatChronology(main.events.Entries, accent);
        main.eventLog.ScrollToLine(main.eventLog.GetLineCount());
    }

    internal void PresentEvents(IReadOnlyList<EventPresentation> presented)
        => motion.Present(presented);

    internal void BeginEventCue(EventPresentation entry, int generation)
        => motion.BeginCue(entry, generation);

    internal void SkipEventPresentation()
        => motion.Skip();

    internal void ReleaseEventTween()
        => motion.ReleaseTween();

    internal void FinishEventPresentation(int generation)
        => motion.Finish(generation);

    internal void SetEventPresentationSettled()
        => motion.SetSettled();

    internal void ApplyProgress(GameProgressPresentation progress)
    {
        main.currentProgress = progress;
        main.title.Text = progress.Title;
        main.description.Text = progress.Description;
        main.status.Text = progress.Status;
        bool danger = progress.Kind is GameProgressKind.VillainWins
            or GameProgressKind.PlayersLose
            or GameProgressKind.DecisionRejected
            or GameProgressKind.SynchronizationUnavailable
            or GameProgressKind.Unconfirmed
            or GameProgressKind.Unavailable
            or GameProgressKind.ServiceUnavailable
            or GameProgressKind.VersionMismatch
            or GameProgressKind.SessionUnavailable
            or GameProgressKind.StorageFailure;
        main.statusPanel.ThemeTypeVariation = danger
            ? GodotThemeVariations.DangerStatusPanel
            : GodotThemeVariations.StatusPanel;
        main.status.ThemeTypeVariation = danger
            ? GodotThemeVariations.DangerText
            : GodotThemeVariations.StatusText;
        main.decisions.SetSubmitting(progress.LocksDecisions);
    }

    internal void RefreshSynchronizeAvailability()
    {
        main.synchronize.Disabled = MutationUnavailable();
        HistoryDescriptor? history = main.CurrentGame?.History;
        int last = (history?.Cursor ?? 0) - 1;
        main.undoLast.Disabled = MutationUnavailable()
            || main.decisionPending
            || !CanUndo(history, last);
        main.undoLast.TooltipText = main.undoLast.Disabled
            ? "New information, a pending action, or table authority prevents undoing the latest action."
            : "Undo the latest completed action.";
    }

    private bool MutationUnavailable() =>
        main.session is null || main.synchronizing || main.resolveInFlight;

    private static bool CanUndo(HistoryDescriptor? history, int cursor) =>
        history?.Undo.Contains(cursor) == true;
}
