using System.Globalization;
using Godot;
using Marvel.Client;
using Marvel.Decisions;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns decision submission, undo, and synchronization workflows.</summary>
internal sealed class MainSessionController
{
    private readonly Main main;

    internal MainSessionController(Main main)
    {
        this.main = main;
    }
    internal async void OnDecisionSubmitted(EngineDecision decision)
    {
        if (main.decisionPending || main.resolveInFlight)
        {
            return;
        }

        main.resolveInFlight = true;
        main.RefreshSynchronizeAvailability();
        try
        {
            main.ApplyProgress(GameProgressPresentation.Resolving());
            main.promptProgress.Text = "RESOLVING  ·  WAITING FOR ENGINE";
            main.promptProgress.ThemeTypeVariation = GodotThemeVariations.StatusText;
            main.transcript.RecordDecision(main.CurrentGame!.Revision, decision);
            ClientResolutionResult result = await main.client!.ResolveAsync(
                main.session!, decision);
            if (!main.IsInsideTree())
            {
                return;
            }
            HandleDecisionResult(result);
        }
        catch (Exception)
        {
            if (main.IsInsideTree())
            {
                main.decisionPending = true;
                main.uncertainMutationError = new ClientStartupError(
                    "display_failed",
                    "The decision result could not be displayed.");
                ShowUnconfirmed(main.uncertainMutationError);
            }
        }
        finally
        {
            main.resolveInFlight = false;
            if (main.IsInsideTree())
            {
                main.RefreshSynchronizeAvailability();
            }
        }
    }

    private void HandleDecisionResult(ClientResolutionResult result)
    {
        if (result.SessionDisposition == ClientSessionDisposition.Unavailable)
        {
            main.ReturnToJoinAfterSessionLoss(result.Error ?? SessionUnavailable());
            return;
        }
        if (result.MutationDisposition == ClientMutationDisposition.NotSent)
        {
            ShowDecisionNotSent(result.Error);
            return;
        }
        if (result.HasAuthoritativeView)
        {
            ShowAuthoritativeDecision(result);
            return;
        }
        ShowUnresolvedDecision(result);
    }

    private void ShowDecisionNotSent(ClientStartupError? error)
    {
        main.decisionPending = false;
        main.uncertainMutationError = null;
        main.ApplyProgress(GameProgressPresentation.DecisionNotSent(error ?? new ClientStartupError(
            "decision_not_sent", "The decision did not reach the game service.")));
        main.promptProgress.Text = "NOT SENT  ·  RETRY SAFE";
        main.promptProgress.ThemeTypeVariation = GodotThemeVariations.StatusText;
    }

    private void ShowAuthoritativeDecision(ClientResolutionResult result)
    {
        if (result.Error is null)
        {
            main.RenderGame(result.Response!);
        }
        else
        {
            main.transcript.RecordFailure(
                EngineProtocol.Resolve, main.CurrentGame!.Revision, result.Error,
                result.MutationDisposition);
            main.RenderGame(
                result.Response!, preserveEvents: true, priorProgress: main.currentProgress,
                operation: EngineProtocol.Sync);
        }
        main.decisionPending = false;
        main.uncertainMutationError = null;
        if (result.Error is not null)
        {
            main.ApplyProgress(GameProgressPresentation.Recovered(result.Response!, result.Error));
        }
    }

    private void ShowUnresolvedDecision(ClientResolutionResult result)
    {
        ClientStartupError failure = result.Error ?? new ClientStartupError(
            "decision_unresolved",
            "The decision result could not be reconciled with the current table.");
        if (result.MutationDisposition != ClientMutationDisposition.Rejected)
        {
            main.decisionPending = true;
            main.uncertainMutationError = failure;
            ShowUnconfirmed(failure);
            return;
        }
        main.decisionPending = false;
        main.uncertainMutationError = null;
        main.ApplyProgress(GameProgressPresentation.DecisionRejected(failure));
        main.promptProgress.Text = "REJECTED  ·  SYNCHRONIZE TABLE";
        main.promptProgress.ThemeTypeVariation = GodotThemeVariations.DangerText;
        main.synchronize.TooltipText = "Read the current authoritative table.";
    }

    private static ClientStartupError SessionUnavailable() => new(
        "session_unavailable",
        "This table session is no longer available. Join again with a new invitation.");

    internal void OnUndoLastPressed()
    {
        HistoryDescriptor? history = main.CurrentGame?.History;
        int target = (history?.Cursor ?? 0) - 1;
        if (history?.Undo.Contains(target) == true)
        {
            UndoTo(target);
        }
    }

    internal void OnHistoryMetaClicked(Variant meta)
    {
        const string prefix = "undo:";
        string value = meta.AsString();
        if (value.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(
                value.AsSpan(prefix.Length),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int cursor)
            && main.CurrentGame?.History?.Undo.Contains(cursor) == true)
        {
            UndoTo(cursor);
        }
    }

    internal async void UndoTo(int cursor)
    {
        if (!CanMutate())
        {
            return;
        }

        main.resolveInFlight = true;
        main.RefreshSynchronizeAvailability();
        try
        {
            main.ApplyProgress(GameProgressPresentation.Resolving());
            main.promptProgress.Text = "UNDOING  ·  VERIFYING HISTORY";
            main.promptProgress.ThemeTypeVariation = GodotThemeVariations.StatusText;
            ClientResolutionResult result = await main.client!.UndoAsync(main.session!, cursor);
            if (!main.IsInsideTree())
            {
                return;
            }

            HandleUndoResult(result);
        }
        catch (Exception)
        {
            if (main.IsInsideTree())
            {
                main.decisionPending = true;
                main.uncertainMutationError = new ClientStartupError(
                    "display_failed",
                    "The undo result could not be displayed.");
                ShowUnconfirmed(main.uncertainMutationError);
            }
        }
        finally
        {
            main.resolveInFlight = false;
            if (main.IsInsideTree())
            {
                main.RefreshSynchronizeAvailability();
            }
        }
    }

    private bool CanMutate() =>
        !main.decisionPending && !main.resolveInFlight && main.client is not null && main.session is not null;

    private void HandleUndoResult(ClientResolutionResult result)
    {
        if (result.SessionDisposition == ClientSessionDisposition.Unavailable)
        {
            main.ReturnToJoinAfterSessionLoss(result.Error ?? SessionUnavailable());
            return;
        }
        if (result.MutationDisposition == ClientMutationDisposition.NotSent)
        {
            ShowUndoNotSent(result.Error);
            return;
        }
        if (result.HasAuthoritativeView)
        {
            ShowAuthoritativeUndo(result);
            return;
        }
        ShowUnresolvedUndo(result);
    }

    private void ShowUndoNotSent(ClientStartupError? error)
    {
        main.ApplyProgress(GameProgressPresentation.DecisionNotSent(error ?? new ClientStartupError(
            "history_not_sent", "The history change did not reach the game service.")));
        main.promptProgress.Text = "UNDO NOT SENT  ·  RETRY SAFE";
        main.promptProgress.ThemeTypeVariation = GodotThemeVariations.StatusText;
    }

    private void ShowAuthoritativeUndo(ClientResolutionResult result)
    {
        main.SkipEventPresentation();
        main.events.Reset([]);
        main.DismissLastResult();
        main.RenderGame(result.Response!, resetEvents: true, operation: EngineProtocol.Undo);
        main.decisionPending = false;
        main.uncertainMutationError = null;
        if (result.Error is not null)
        {
            main.ApplyProgress(GameProgressPresentation.Recovered(result.Response!, result.Error));
        }
    }

    private void ShowUnresolvedUndo(ClientResolutionResult result)
    {
        ClientStartupError failure = result.Error ?? new ClientStartupError(
            "history_unresolved", "The undo result could not be reconciled with the current table.");
        if (result.MutationDisposition != ClientMutationDisposition.Rejected)
        {
            main.decisionPending = true;
            main.uncertainMutationError = failure;
            ShowUnconfirmed(failure);
            return;
        }
        main.ApplyProgress(GameProgressPresentation.DecisionRejected(failure));
        main.promptProgress.Text = "UNDO REJECTED  ·  SYNCHRONIZE TABLE";
        main.promptProgress.ThemeTypeVariation = GodotThemeVariations.DangerText;
    }

    internal void ShowUnconfirmed(ClientStartupError error)
    {
        main.ApplyProgress(GameProgressPresentation.Unconfirmed(error));
        main.promptProgress.Text =
            $"UNCONFIRMED  ·  {error.Code.ToUpperInvariant()}  ·  RESTART OR RECONNECT";
        main.promptProgress.ThemeTypeVariation = GodotThemeVariations.DangerText;
        main.syncStatus.Text = "⚠ Sync needed";
        main.synchronize.TooltipText = "Reconnect to the current authoritative table.";
    }

    internal async void OnSynchronizePressed()
    {
        if (!CanSynchronize())
        {
            return;
        }

        GameProgressPresentation prior = main.currentProgress
            ?? GameProgressPresentation.FromResponse(main.CurrentGame!);
        bool hadUncertainMutation = main.decisionPending;
        main.synchronizing = true;
        main.RefreshSynchronizeAvailability();
        main.ApplyProgress(GameProgressPresentation.Synchronizing());
        try
        {
            ClientSynchronizationResult result = await main.client!.SynchronizeAsync(main.session!);
            if (!main.IsInsideTree())
            {
                return;
            }

            HandleSynchronizationResult(result, prior, hadUncertainMutation);
        }
        catch (Exception)
        {
            if (main.IsInsideTree())
            {
                ApplySynchronizationFailure(new ClientStartupError(
                    "synchronization_failed",
                    "The current table could not be read. Try reconnecting again."),
                    prior,
                    hadUncertainMutation);
            }
        }
        finally
        {
            main.synchronizing = false;
            if (main.IsInsideTree())
            {
                main.RefreshSynchronizeAvailability();
            }
        }
    }

    private bool CanSynchronize() =>
        !main.synchronizing && !main.resolveInFlight && main.client is not null && main.session is not null;

    private void HandleSynchronizationResult(
        ClientSynchronizationResult result,
        GameProgressPresentation prior,
        bool hadUncertainMutation)
    {
        if (result.Succeeded)
        {
            main.decisionPending = false;
            main.uncertainMutationError = null;
            main.RenderGame(
                result.Response!, preserveEvents: true, priorProgress: prior,
                operation: EngineProtocol.Sync);
            main.synchronize.TooltipText = "Read the current authoritative table.";
            return;
        }
        if (result.SessionDisposition == ClientSessionDisposition.Unavailable)
        {
            main.ReturnToJoinAfterSessionLoss(result.Error!);
            return;
        }
        ApplySynchronizationFailure(result.Error!, prior, hadUncertainMutation);
    }

    internal void ApplySynchronizationFailure(
        ClientStartupError error,
        GameProgressPresentation prior,
        bool hadUncertainMutation)
    {
        if (hadUncertainMutation)
        {
            main.decisionPending = true;
            ShowUnconfirmed(main.uncertainMutationError ?? error);
        }
        else
        {
            main.ApplyProgress(GameProgressPresentation.SynchronizationUnavailable(
                error,
                prior.LocksDecisions,
                prior.OperationalLock));
        }
        main.syncStatus.Text = "⚠ Sync needed";
        main.synchronize.TooltipText = "Reconnect to the current authoritative table.";
    }

    internal void ReturnToJoinAfterSessionLoss(ClientStartupError error)
    {
        main.session = null;
        main.client = null;
        main.CurrentGame = null;
        main.transientInvitation = null;
        main.invitation.Clear();
        main.invitationOffer.Visible = false;
        main.boardRender = null;
        main.events.Reset([]);
        main.activeResolution.Visible = false;
        main.lastResult.Visible = false;
        main.lastResultGeneration++;
        main.RenderEvents();
        main.boardAreas.GetChildren().ToList().ForEach(node => node.QueueFree());
        main.board.Visible = false;
        main.setupPanel.Visible = true;
        main.decisionPending = false;
        main.resolveInFlight = false;
        main.uncertainMutationError = null;
        main.synchronize.TooltipText = "Read the current authoritative table.";
        main.synchronize.Disabled = true;
        main.synchronize.Visible = false;
        main.syncStatus.Visible = false;
        main.cardInspector.Visible = false;
        main.SetSetupControlsEnabled(true);
        main.ShowEntryMode(joinMode: true);
        main.ShowFailure(error);
    }
}
