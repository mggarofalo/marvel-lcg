using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Returns a client with a lost session to a safe join-ready entry state.</summary>
internal sealed class MainSessionLossRecovery
{
    private readonly Main main;

    internal MainSessionLossRecovery(Main main)
    {
        this.main = main;
    }

    internal void ReturnToJoin(ClientStartupError error)
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
