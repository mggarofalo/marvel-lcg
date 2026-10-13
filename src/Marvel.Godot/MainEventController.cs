using Godot;
using Marvel.Client;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns prompt summaries, event chronology, and progress presentation.</summary>
internal sealed class MainEventController : IDisposable
{
    private readonly Main main;
    private readonly HistoryAccountController history;
    private readonly MainEventMotionController motion;
    private readonly MainResolutionStageController resolutionStage;
    private readonly EventTimelineController timeline;

    internal MainEventController(Main main)
    {
        this.main = main;

        motion = new MainEventMotionController(main);
        resolutionStage = new MainResolutionStageController(main);
        timeline = new EventTimelineController(main);
        history = new HistoryAccountController(main, timeline);
    }
    internal void RenderPromptSummary(Prompt? prompt, WorldDescriptor world)
    {
        if (!main.boardController.ResponseStaged)
            PresentAttack(AttackActivityPresentation.From(world.ActiveAttack));
        if (prompt is null)
        {
            resolutionStage.Clear();
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
        resolutionStage.Render(view);
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

        main.lastResultGeneration++;
        main.lastResult.Visible = true;
        SetLastResultText(string.Join(" ", highlights.Select(entry => entry.Summary)));
        main.lastResult.ThemeTypeVariation = highlights.Any(entry => entry.Motion is
            EventMotionKind.Defeat or EventMotionKind.Terminal)
                ? GodotThemeVariations.DangerStatusPanel
                : GodotThemeVariations.StatusPanel;
        // The latest authoritative result remains readable while the next choice is available.
        // A new accepted result, an explicit dismissal or a session reset replaces it.
        SetLastResultExpanded(true);
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
        motion.Skip();
        main.lastResultGeneration++;
        main.lastResultExpanded = false;
        main.lastResult.Visible = false;
        main.lastResultSummary.Visible = false;
        SetLastResultText(string.Empty);
        main.lastResultToggle.Text = "Expand";
    }

    private void SetLastResultText(string text)
    {
        main.lastResultSummary.Text = text;
        main.boardRender?.PresentLastResult(text);
        if (main.promptPanel.FindChild("LatestResult", recursive: true, owned: false)
            is Label historyResult)
        {
            TableLatestResult.Present(historyResult, text);
        }
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

    internal event Action? PlaybackSettled
    { add => motion.Settled += value; remove => motion.Settled -= value; }

    internal void RenderEvents() => history.Render();

    internal void RecordTimeline(EngineResponse response, IReadOnlyList<EventPresentation> entries, bool reset)
        { timeline.Record(response, entries, reset); history.Render(); }

    internal void FitTimeline() => timeline.Fit();
    internal void ClearTimeline() => timeline.Clear();
    internal void PresentTimelineBeat(EventActivitySequence sequence) => timeline.PresentBeat(sequence);

    internal void PresentEvents(IReadOnlyList<EventPresentation> presented)
        => motion.Present(presented);

    internal void PresentAttack(EventPresentation? attack) => motion.PresentAttack(attack);

    internal void BeginEventCue(EventPresentation entry, int generation)
        => motion.BeginCue(entry, generation);

    internal void SkipEventPresentation()
        => motion.Skip();

    internal void PauseEventPresentation() => motion.Pause();

    internal void CompleteEventPresentation() => motion.Complete();

    internal void ReleaseEventTween()
        => motion.ReleaseTween();

    internal void FinishEventPresentation(int generation)
        => motion.Finish(generation);

    internal void SetEventPresentationSettled()
        => motion.SetSettled();

    internal void RefreshEventCueVisibility() => motion.RefreshVisibility();

    internal void ApplyProgress(GameProgressPresentation progress)
    {
        main.currentProgress = progress;
        main.decisions.PresentOperationalNotice(progress);
        main.title.Text = progress.Title;
        main.description.Text = progress.Description;
        main.status.Text = main.boardController.ResponseStaged ? "RESOLVING" : progress.Status;
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
        main.decisions.SetSubmitting(progress.LocksDecisions || main.boardController.ResponseStaged);
        TableSidebarContent.RefreshProgress(main);
    }

    internal void RefreshSynchronizeAvailability()
    {
        main.synchronize.Disabled = !main.lifecycle.CanSynchronize;
        HistoryUndoControl.Refresh(main);
    }

    public void Dispose()
    {
        motion.Dispose();
        timeline.Dispose();
    }

}
