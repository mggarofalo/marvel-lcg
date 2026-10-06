using System.Globalization;
using Godot;
using Marvel.Client;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns setup discovery, entry-mode choices, and session creation.</summary>
internal sealed class MainSetupController
{
    private readonly Main main;
    private readonly SetupSelectionControls selection;

    internal MainSetupController(Main main)
    {
        this.main = main;
        selection = new SetupSelectionControls(main);
    }
    internal async Task LoadSetupAsync()
    {
        long generation = main.lifecycle.BeginEntry();
        string requestedEndpoint = main.endpoint.Text;
        main.setupChoices = null;
        main.reloadSetup.Disabled = true;
        main.start.Disabled = true;
        main.SetAssignmentControlsEnabled(false);
        main.status.Text = "Loading setup choices…";
        main.statusPanel.Visible = true;
        try
        {
            LocalClientConnection connection = ClientComposition.Connect(
                DesktopDataRoot.Current(),
                requestedEndpoint);
            if (!connection.Succeeded)
            {
                ApplySetupFailure(generation, requestedEndpoint, connection.Error!);
                return;
            }

            LocalGameClient candidate = connection.Client!;
            ClientSetupResult setup = await candidate.ReadSetupAsync();
            if (!IsCurrentSetupLoad(generation, requestedEndpoint))
            {
                return;
            }

            if (!setup.Succeeded)
            {
                ApplySetupFailure(generation, requestedEndpoint, setup.Error!);
                return;
            }

            main.setupChoices = setup.Choices;
            main.lifecycle.FinishEntry(generation);
            PopulateSetupChoices();
            RefreshEntryAvailability();
            main.reloadSetup.Disabled = false;
            main.title.Text = "Assemble the table.";
            main.description.Text = string.Empty;
            main.status.Text = string.Empty;
            main.statusPanel.Visible = false;
            main.statusPanel.ThemeTypeVariation = GodotThemeVariations.StatusPanel;
            main.status.ThemeTypeVariation = GodotThemeVariations.StatusText;
        }
        catch (Exception)
        {
            ApplySetupFailure(generation, requestedEndpoint, new ClientStartupError(
                "startup_failed",
                "The setup options could not be loaded. Check the endpoint and try again."));
        }
    }

    internal bool IsCurrentSetupLoad(long generation, string requestedEndpoint) =>
        main.IsInsideTree() && main.lifecycle.IsCurrentEntry(generation)
        && main.endpoint.Text == requestedEndpoint;

    internal void ApplySetupFailure(
        long generation,
        string requestedEndpoint,
        ClientStartupError error)
    {
        if (!IsCurrentSetupLoad(generation, requestedEndpoint))
        {
            return;
        }

        main.lifecycle.FinishEntry(generation);
        main.reloadSetup.Disabled = false;
        main.ShowFailure(error);
    }

    internal void OnEndpointChanged()
    {
        main.lifecycle.Detach();
        main.setupChoices = null;
        main.SetAssignmentControlsEnabled(false);
        main.reloadSetup.Disabled = false;
        main.status.Text = "Endpoint changed. Reload the setup choices.";
        main.statusPanel.Visible = true;
        RefreshEntryAvailability();
    }

    internal void PopulateSetupChoices() => selection.PopulateSetupChoices();
    internal void PopulateSecondHeroChoices() => selection.PopulateSecondHeroChoices();
    internal void OnScenarioSelected(long selected) => selection.OnScenarioSelected(selected);
    internal void PopulateModularChoices() => selection.PopulateModularChoices();
    internal void OnModularChoicePressed(long id) => selection.OnModularChoicePressed(id);
    internal void RefreshModularControl() => selection.RefreshModularControl();
    internal void RefreshBriefing() => selection.RefreshBriefing();
    internal void RefreshStartAvailability() => selection.RefreshStartAvailability();
    internal void RefreshEntryAvailability() => selection.RefreshEntryAvailability();

    internal async void OnStartPressed()
    {
        if (main.start.Disabled) return;
        long generation = main.lifecycle.BeginEntry();
        string requestedEndpoint = main.endpoint.Text;
        try
        {
            main.start.Disabled = true;
            main.SetSetupControlsEnabled(false);
            main.status.Text = "DEALING GAME  ·  ONE MOMENT";

            if (string.IsNullOrWhiteSpace(main.seed.Text))
            {
                main.seed.Text = GameSeed.Create().ToString(CultureInfo.InvariantCulture);
            }

            GameSetupSelection selection = main.SelectedSetup();
            LocalClientConnection connection = ClientComposition.Connect(
                DesktopDataRoot.Current(),
                main.endpoint.Text);
            if (!connection.Succeeded)
            {
                RestoreEntryAfterFailure(generation, connection.Error!);
                return;
            }

            LocalGameClient candidate = connection.Client!;
            ClientSetupResult available = await candidate.ReadSetupAsync();
            if (!IsCurrentSetupLoad(generation, requestedEndpoint)) return;
            if (!available.Succeeded)
            {
                RestoreEntryAfterFailure(generation, available.Error!);
                return;
            }

            ClientEntryResult startup = await candidate.OpenSessionAsync(
                main.gameId.Text, available.Choices!, selection);
            if (!IsCurrentSetupLoad(generation, requestedEndpoint)) return;
            if (!startup.Succeeded)
            {
                RestoreEntryAfterFailure(generation, startup.Error!);
                return;
            }

            if (!main.lifecycle.Enter(generation, candidate, startup)) return;
            ShowStartedGame(startup, available.Choices!, selection);
        }
        catch (Exception)
        {
            RestoreEntryAfterFailure(generation, new ClientStartupError(
                "startup_failed",
                "The selected game could not be displayed. Check the assignment and try again."));
        }
    }

    private void ShowStartedGame(ClientEntryResult startup, SetupChoices available, GameSetupSelection selection)
    {
        main.boardController.ResetForSession();
        main.currentProgress = null;
        main.transcript.Reset(
            uint.Parse(main.seed.Text, CultureInfo.InvariantCulture),
            available.Runtime,
            InteractionTranscriptSetup.FromSelection(available, selection));
        main.transientInvitation = startup.Invitations.Count == 0
            ? null
            : startup.Invitations[0].Invitation;
        main.invitationOffer.Visible = main.transientInvitation is not null;
        main.boardController.RenderGame(startup.Response!, resetEvents: true, operation: EngineProtocol.Open);
        main.setupPanel.Visible = false;
        main.board.Visible = true;
        main.eyebrow.Text = main.endpoint.Text.Length == 0
            ? $"CORE SET  /  EMBEDDED TABLE  /  SEED {main.seed.Text}"
            : $"CORE SET  /  HOSTED TABLE  /  SEED {main.seed.Text}";
        main.title.ThemeTypeVariation = GodotThemeVariations.BriefingTitle;
        main.ApplyResponsivePlayLayout();
        main.description.Visible = true;
        main.pageScroll.ScrollVertical = 0;
        main.pageScroll.SetDeferred("scroll_vertical", 0);
    }

    internal async void OnJoinPressed()
    {
        if (main.join.Disabled)
        {
            return;
        }

        long generation = main.lifecycle.BeginEntry();
        string requestedEndpoint = main.endpoint.Text;
        string secret = main.invitation.Text;
        main.invitation.Clear();
        try
        {
            main.join.Disabled = true;
            main.SetSetupControlsEnabled(false);
            main.status.Text = "JOINING GAME  ·  ONE MOMENT";
            LocalClientConnection connection = ClientComposition.Connect(
                DesktopDataRoot.Current(),
                main.endpoint.Text);
            if (!connection.Succeeded)
            {
                RestoreEntryAfterFailure(generation, connection.Error!);
                return;
            }

            LocalGameClient candidate = connection.Client!;
            ClientSetupResult available = await candidate.ReadSetupAsync();
            if (!IsCurrentSetupLoad(generation, requestedEndpoint)) return;
            if (!available.Succeeded)
            {
                RestoreEntryAfterFailure(generation, available.Error!);
                return;
            }
            ClientEntryResult attached = await candidate.AttachAsync(main.gameId.Text, secret);
            secret = string.Empty;
            if (!IsCurrentSetupLoad(generation, requestedEndpoint)) return;
            if (!attached.Succeeded)
            {
                RestoreEntryAfterFailure(generation, attached.Error!);
                return;
            }

            if (!main.lifecycle.Enter(generation, candidate, attached)) return;
            main.boardController.ResetForSession();
            main.currentProgress = null;
            main.transcript.Reset(
                seed: null,
                runtime: available.Choices!.Runtime,
                InteractionTranscriptSetup.Unavailable("unavailable_to_attached_viewer"));
            main.boardController.RenderGame(
                attached.Response!,
                resetEvents: true,
                operation: EngineProtocol.Attach);
            main.setupPanel.Visible = false;
            main.board.Visible = true;
            main.eyebrow.Text = "CORE SET  /  JOINED TABLE";
            main.title.ThemeTypeVariation = GodotThemeVariations.BriefingTitle;
            main.ApplyResponsivePlayLayout();
            main.description.Visible = true;
            main.pageScroll.ScrollVertical = 0;
            main.pageScroll.SetDeferred("scroll_vertical", 0);
        }
        catch (Exception)
        {
            secret = string.Empty;
            RestoreEntryAfterFailure(generation, new ClientStartupError(
                "startup_failed",
                "The invitation could not be attached. Check the endpoint and ask the host for a new invitation."));
        }
    }

    internal void CopyInvitation()
    {
        if (main.transientInvitation is null)
        {
            return;
        }

        DisplayServer.ClipboardSet(main.transientInvitation);
        main.transientInvitation = null;
        main.invitationOffer.Visible = false;
        main.status.Text = "INVITATION COPIED  ·  THE ONE-TIME SECRET WAS REMOVED FROM THIS SCREEN";
    }

    internal void CopyInteractionReport()
    {
        DisplayServer.ClipboardSet(main.transcript.Export());
        main.status.Text = "INTERACTION REPORT COPIED  ·  AUTHORIZED GAME CONTENT INCLUDED";
    }

    internal void SaveInteractionReport()
    {
        string path = Path.Combine(OS.GetUserDataDir(), "marvel-interaction-report.json");
        File.WriteAllText(path, main.transcript.Export());
        main.status.Text = $"INTERACTION REPORT SAVED  ·  {path}";
    }

    private void RestoreEntryAfterFailure(long generation, ClientStartupError error)
    {
        if (!main.IsInsideTree() || !main.lifecycle.IsCurrentLifetime(generation)) return;
        if (!main.lifecycle.FinishEntry(generation))
        {
            main.lifecycle.PresentationFailed();
            main.sessionController.ShowRecoveryControls();
            main.ApplyProgress(main.lifecycle.Progress!);
            return;
        }
        main.SetSetupControlsEnabled(true);
        RefreshEntryAvailability();
        main.ShowFailure(error);
    }
}
