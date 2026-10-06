using System.Globalization;
using Godot;
using Marvel.Client;
using Marvel.Decisions;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Connects session lifecycle results to table rendering and the sole decision draft.</summary>
internal sealed class MainSessionController
{
    private readonly Main main;
    private readonly MainSessionLossRecovery recovery;

    internal MainSessionController(Main main)
    {
        this.main = main;
        recovery = new MainSessionLossRecovery(main);
    }

    internal async void OnDecisionSubmitted(EngineDecision decision)
    {
        if (!main.lifecycle.CanResolve) return;
        EngineResponse current = main.CurrentGame!;
        DecisionReceiptContext? receipt = current.Prompt is { } prompt
            ? DecisionReceiptContext.From(prompt, current.World!, decision.Affordance,
                decision.Targets, decision.Resources ?? []) : null;
        main.transcript.RecordDecision(current.Revision, decision);
        await PresentOperation(main.lifecycle.ResolveAsync(current.Revision, decision),
            EngineProtocol.Resolve, receipt);
    }

    internal void OnUndoLastPressed()
    {
        int target = (main.CurrentGame?.History?.Cursor ?? 0) - 1;
        UndoTo(target);
    }

    internal void OnHistoryMetaClicked(Variant meta)
    {
        const string prefix = "undo:";
        string value = meta.AsString();
        if (value.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(value.AsSpan(prefix.Length), NumberStyles.None,
                CultureInfo.InvariantCulture, out int cursor)) UndoTo(cursor);
    }

    internal async void UndoTo(int cursor)
    {
        if (main.decisions.PaymentModalOpen || !main.lifecycle.CanUndo(cursor)) return;
        await PresentOperation(main.lifecycle.UndoAsync(cursor), EngineProtocol.Undo);
    }

    internal async void OnSynchronizePressed()
    {
        if (!main.lifecycle.CanSynchronize) return;
        bool hadDraft = main.decisions.composer?.Selected is not null;
        await PresentOperation(main.lifecycle.SynchronizeAsync(hadDraft), EngineProtocol.Sync);
    }

    private async Task PresentOperation(
        Task<ClientLifecycleUpdate?> operation, string kind, DecisionReceiptContext? receipt = null)
    {
        try
        {
            bool sendingDisplayed = TryPresentSending();
            ClientLifecycleUpdate? update = await operation;
            if (!sendingDisplayed && update is not null)
                throw new InvalidOperationException("The sending state could not be displayed.");
            if (!main.IsInsideTree() || update is null) return;
            Present(update, kind, receipt);
        }
        catch (Exception)
        {
            if (main.IsInsideTree())
            {
                main.lifecycle.PresentationFailed();
                ShowRecoveryControls();
                if (main.lifecycle.Progress is { } progress) main.ApplyProgress(progress);
            }
        }
        finally
        {
            if (main.IsInsideTree()) main.RefreshSynchronizeAvailability();
        }
    }

    internal void ShowRecoveryControls()
    {
        main.synchronize.Visible = true;
        main.syncStatus.Visible = true;
        main.syncStatus.Text = "⚠ Sync needed";
        main.synchronize.TooltipText = "Read the current authoritative table.";
        main.RefreshSynchronizeAvailability();
    }

    private bool TryPresentSending()
    {
        // Observe the request even if its indicator cannot be drawn; local display
        // failure cannot cancel or roll back a dispatched mutation.
        try
        {
            main.ApplyProgress(main.lifecycle.Progress!);
            main.RefreshSynchronizeAvailability();
            return true;
        }
        catch (Exception) { return false; }
    }

    private void Present(ClientLifecycleUpdate update, string kind, DecisionReceiptContext? receipt)
    {
        if (update.Draft == ClientDraftDisposition.Clear)
        {
            ReturnToJoinAfterSessionLoss(update.Error!);
            return;
        }
        if (update.Error is not null && update.MutationDisposition is { } disposition)
            main.transcript.RecordFailure(kind, main.CurrentGame!.Revision, update.Error, disposition);
        RenderResponse(update, kind, receipt);
        PresentDraftState(update);
    }

    private void RenderResponse(ClientLifecycleUpdate update, string kind, DecisionReceiptContext? receipt)
    {
        if (update.Response is { } response)
        {
            if (kind == EngineProtocol.Undo)
            {
                main.SkipEventPresentation();
                main.events.Reset([]);
                main.DismissLastResult();
            }
            main.boardController.RenderGame(response,
                resetEvents: kind == EngineProtocol.Undo,
                preserveEvents: kind == EngineProtocol.Sync || update.Error is not null,
                operation: update.Error is null ? kind : EngineProtocol.Sync,
                acceptedReceipt: update.MutationAccepted ? receipt : null);
        }
    }

    private void PresentDraftState(ClientLifecycleUpdate update)
    {
        if (update.Draft == ClientDraftDisposition.Replace)
            main.decisions.AuthoritativeSynchronization(main.CurrentGame!.Revision);
        else if (update.Draft == ClientDraftDisposition.Retry)
            main.decisions.AllowRetry(main.CurrentGame!.Revision);
        main.ApplyProgress(update.Progress);
        if (update.Response is null)
        {
            main.promptProgress.Text = update.Progress.Status;
            main.promptProgress.ThemeTypeVariation = update.Progress.LocksDecisions
                ? GodotThemeVariations.DangerText : GodotThemeVariations.StatusText;
        }
        if (update.Response is null && update.Error is not null)
            main.syncStatus.Text = update.Progress.Kind == GameProgressKind.DecisionNotSent
                ? "Not sent · retry safe" : "⚠ Sync needed";
        main.synchronize.TooltipText = "Read the current authoritative table.";
    }

    internal void ReturnToJoinAfterSessionLoss(ClientStartupError error) => recovery.ReturnToJoin(error);
}
