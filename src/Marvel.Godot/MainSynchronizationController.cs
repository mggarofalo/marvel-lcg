using Marvel.Client;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns authoritative synchronization and its locked-input recovery.</summary>
internal sealed class MainSynchronizationController
{
    private readonly Main main;

    internal MainSynchronizationController(Main main)
    {
        this.main = main;
    }

    internal async void OnPressed()
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

            HandleResult(result, prior, hadUncertainMutation);
        }
        catch (Exception)
        {
            if (main.IsInsideTree())
            {
                ApplyFailure(new ClientStartupError(
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

    internal void ApplyFailure(
        ClientStartupError error,
        GameProgressPresentation prior,
        bool hadUncertainMutation)
    {
        if (hadUncertainMutation)
        {
            main.decisionPending = true;
            main.ShowUnconfirmed(main.uncertainMutationError ?? error);
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

    private bool CanSynchronize() =>
        !main.synchronizing && !main.resolveInFlight && main.client is not null && main.session is not null;

    private void HandleResult(
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
            main.decisions.AuthoritativeSynchronization(result.Response!.Revision);
            main.synchronize.TooltipText = "Read the current authoritative table.";
            return;
        }
        if (result.SessionDisposition == ClientSessionDisposition.Unavailable)
        {
            main.ReturnToJoinAfterSessionLoss(result.Error!);
            return;
        }
        ApplyFailure(result.Error!, prior, hadUncertainMutation);
    }
}
