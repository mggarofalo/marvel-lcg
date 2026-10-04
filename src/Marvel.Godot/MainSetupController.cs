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
        int generation = ++main.setupLoadGeneration;
        string requestedEndpoint = main.endpoint.Text;
        main.setupLoading = true;
        main.setupChoices = null;
        main.client = null;
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

            main.client = candidate;
            main.setupChoices = setup.Choices;
            PopulateSetupChoices();
            main.setupLoading = false;
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

    internal bool IsCurrentSetupLoad(int generation, string requestedEndpoint) =>
        generation == main.setupLoadGeneration
        && main.endpoint.Text == requestedEndpoint;

    internal void ApplySetupFailure(
        int generation,
        string requestedEndpoint,
        ClientStartupError error)
    {
        if (!IsCurrentSetupLoad(generation, requestedEndpoint))
        {
            return;
        }

        main.setupLoading = false;
        main.reloadSetup.Disabled = false;
        main.ShowFailure(error);
    }

    internal void OnEndpointChanged()
    {
        main.setupLoadGeneration++;
        main.setupLoading = false;
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
                RestoreEntryAfterFailure(connection.Error!);
                return;
            }

            main.client = connection.Client;
            ClientSetupResult available = await main.client!.ReadSetupAsync();
            if (!available.Succeeded)
            {
                RestoreEntryAfterFailure(available.Error!);
                return;
            }

            ClientEntryResult startup = await main.client.OpenSessionAsync(
                main.gameId.Text, available.Choices!, selection);
            if (!startup.Succeeded)
            {
                RestoreEntryAfterFailure(startup.Error!);
                return;
            }

            main.session = startup.Session;
            main.currentProgress = null;
            main.transcript.Reset(
                uint.Parse(main.seed.Text, CultureInfo.InvariantCulture),
                available.Choices!.Runtime,
                InteractionTranscriptSetup.FromSelection(available.Choices, selection));
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
        catch (Exception)
        {
            main.SetSetupControlsEnabled(true);
            RefreshStartAvailability();
            main.ShowFailure(new ClientStartupError(
                "startup_failed",
                "The selected game could not be displayed. Check the assignment and try again."));
        }
    }

    internal async void OnJoinPressed()
    {
        if (main.join.Disabled)
        {
            return;
        }

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
                RestoreEntryAfterFailure(connection.Error!);
                return;
            }

            main.client = connection.Client;
            ClientSetupResult available = await main.client!.ReadSetupAsync();
            if (!available.Succeeded)
            {
                RestoreEntryAfterFailure(available.Error!);
                return;
            }
            ClientEntryResult attached = await main.client!.AttachAsync(main.gameId.Text, secret);
            secret = string.Empty;
            if (!attached.Succeeded)
            {
                RestoreEntryAfterFailure(attached.Error!);
                return;
            }

            main.session = attached.Session;
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
            RestoreEntryAfterFailure(new ClientStartupError(
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

    internal void RestoreEntryAfterFailure(ClientStartupError error)
    {
        main.SetSetupControlsEnabled(true);
        RefreshEntryAvailability();
        main.ShowFailure(error);
    }
}
