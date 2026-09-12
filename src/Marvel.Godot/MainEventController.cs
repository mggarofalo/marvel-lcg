using System.Globalization;
using System.Text;
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

    internal MainEventController(Main main)
    {
        this.main = main;
    }
    internal void RevealOutcome()
    {
        main.pageScroll.ScrollVertical = 0;
        main.pageScroll.SetDeferred("scroll_vertical", 0);
    }

    internal void RenderPromptSummary(Prompt? prompt, WorldDescriptor world)
    {
        if (prompt is null)
        {
            main.activeResolution.Visible = false;
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
        var text = new StringBuilder();
        IReadOnlyList<int> undo = main.CurrentGame!.History!.Undo;
        foreach (HistoryEntryDescriptor entry in actions)
        {
            AppendAction(text, entry, accent, undo.Contains(entry.Cursor));
        }
        main.eventLog.Text = text.Length == 0 ? "Action in progress." : text.ToString();
        main.eventLog.ScrollToLine(main.eventLog.GetLineCount());
    }

    private static void AppendAction(
        StringBuilder text,
        HistoryEntryDescriptor entry,
        string accent,
        bool canUndo)
    {
        text.Append("[color=#").Append(accent).Append(']')
            .Append((entry.Cursor + 1).ToString("000", CultureInfo.InvariantCulture))
            .Append("[/color]  ").AppendLine(entry.Summary);
        foreach (string detail in entry.Details)
        {
            text.Append("     ").AppendLine(detail);
        }
        if (canUndo)
        {
            text.Append("     [url=undo:")
                .Append(entry.Cursor.ToString(CultureInfo.InvariantCulture))
                .AppendLine("]Undo to before this action[/url]");
        }
    }

    private void RenderEventChronology()
    {
        string accent = ClientTheme.ToGodot(VisualSystem.Palette.Accent).ToHtml(false);
        var text = new StringBuilder();
        for (int index = 0; index < main.events.Entries.Count; index++)
        {
            EventPresentation entry = main.events.Entries[index];
            text.Append("[color=#")
                .Append(accent)
                .Append(']')
                .Append((index + 1).ToString("000", CultureInfo.InvariantCulture))
                .Append("[/color]  ")
                .AppendLine(entry.Summary);
        }

        main.eventLog.Text = text.ToString();
        main.eventLog.ScrollToLine(main.eventLog.GetLineCount());
    }

    internal void PresentEvents(IReadOnlyList<EventPresentation> presented)
    {
        SkipEventPresentation();
        if (!main.eventMotion.ButtonPressed || presented.Count == 0)
        {
            return;
        }

        main.eventSkip.Disabled = false;
        int generation = main.eventGeneration;
        main.eventTween = main.CreateTween();
        foreach (EventPresentation entry in presented)
        {
            main.eventTween.TweenCallback(
                Callable.From(() => BeginEventCue(entry, generation))).Dispose();
            main.eventTween.TweenProperty(main.eventCue, "modulate:a", 1.0f, 0.10).Dispose();
            main.eventTween.TweenInterval(0.30).Dispose();
            main.eventTween.TweenProperty(main.eventCue, "modulate:a", 0.35f, 0.10).Dispose();
        }

        main.eventTween.TweenCallback(
            Callable.From(() => FinishEventPresentation(generation))).Dispose();
    }

    internal void BeginEventCue(EventPresentation entry, int generation)
    {
        if (generation != main.eventGeneration)
        {
            return;
        }

        main.eventCueKind.Text = entry.Motion.ToString().ToUpperInvariant();
        main.eventCue.Visible = true;
        main.eventCueSummary.Text = entry.Summary;
        main.eventCueKind.ThemeTypeVariation = entry.Motion switch
        {
            EventMotionKind.Damage or EventMotionKind.Defeat or EventMotionKind.Terminal =>
                GodotThemeVariations.DangerText,
            EventMotionKind.Create or EventMotionKind.Heal =>
                GodotThemeVariations.StatusText,
            _ => GodotThemeVariations.Eyebrow,
        };
        main.eventCue.Modulate = new Color(1f, 1f, 1f, 0.20f);
        main.boardRender?.Present(entry.Anchors);
    }

    internal void SkipEventPresentation()
    {
        main.eventGeneration++;
        ReleaseEventTween();
        SetEventPresentationSettled();
    }

    internal void ReleaseEventTween()
    {
        Tween? tween = main.eventTween;
        main.eventTween = null;
        if (tween is null)
        {
            return;
        }

        tween.Kill();
        tween.Dispose();
    }

    internal void FinishEventPresentation(int generation)
    {
        if (generation != main.eventGeneration)
        {
            return;
        }

        SetEventPresentationSettled();
    }

    internal void SetEventPresentationSettled()
    {
        main.eventCue.Visible = false;
        main.eventCue.Modulate = Colors.White;
        main.eventSkip.Disabled = true;
        main.boardRender?.Present([]);
    }

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
