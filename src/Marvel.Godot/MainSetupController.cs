using Marvel.Desktop;
using Marvel.Protocol;
using System.Globalization;
using Godot;
using Marvel.Client;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns setup discovery, entry-mode choices, and session creation.</summary>
internal sealed class MainSetupController
{
    private readonly Main main;
    private readonly SetupSelectionControls selection;
    private readonly SetupDiscoveryController discovery;

    internal MainSetupController(Main main)
    {
        this.main = main;
        selection = new SetupSelectionControls(main);
        discovery = new SetupDiscoveryController(main, this);
        new MainShutdownController(main).Attach();
    }
    internal Task LoadSetupAsync() => discovery.LoadAsync();

    internal GameSetupSelection SelectDecks(GameSetupSelection setup) => selection.ApplyDecks(setup);
    internal ScenarioSetupChoice SelectedCampaign() => selection.SelectedCampaign();
    internal IReadOnlyList<OptionButton> VariantControls => selection.VariantControls;
    internal IReadOnlyList<OptionButton> AdditionalHeroes => selection.AdditionalHeroes;
    internal IReadOnlyList<string> SelectedHeroes() => selection.SelectedHeroes();
    internal void AssignHero(string key, int seat) => selection.AssignHero(key, seat);
    internal void RefreshHeroes() => selection.RefreshHeroes();

    internal bool IsCurrentSetupLoad(long generation, string requestedEndpoint) =>
        InteractionControl.IsUsable(main) && main.lifecycle.IsCurrentEntry(generation)
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
        RefreshEntryAvailability();
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
    internal void RefreshEntryAvailability()
    {
        selection.RefreshEntryAvailability();
        if (main.lifecycle.EntryRecovery is not { } recovery) return;
        main.SetSetupControlsEnabled(false);
        bool pendingJoin = recovery.IsAttachment;
        main.start.Text = pendingJoin ? "Start game" : "Recover table";
        main.join.Text = pendingJoin ? "Recover seat" : "Join game";
        main.start.Disabled = pendingJoin;
        main.join.Disabled = !pendingJoin;
    }

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
            var prepared = await PrepareEntryAsync(generation, requestedEndpoint);
            if (prepared is null) return;
            using SetupConnectionLease candidate = prepared.Value.Lease;
            SetupChoices choices = prepared.Value.Choices;
            ClientEntryResult startup = main.lifecycle.EntryRecovery is { } pending
                ? await candidate.Client.RetryEntryAsync(pending)
                : await candidate.Client.OpenSessionAsync(main.gameId.Text, choices, selection);
            if (!InstallEntry(generation, requestedEndpoint, candidate, startup)) return;
            ShowStartedGame(startup, choices, selection);
        }
        catch (Exception)
        {
            RestoreEntryAfterFailure(generation, new ClientStartupError(
                "startup_failed",
                "The selected game could not be displayed. Check the assignment and try again."));
        }
    }

    private async Task<(SetupConnectionLease Lease, SetupChoices Choices)?> PrepareEntryAsync(
        long generation, string requestedEndpoint)
    {
        LocalClientConnection connection = main.lifecycle.EntryRecoveryClient is { } retained
            ? new LocalClientConnection(retained, null)
            : ClientComposition.Connect(DesktopDataRoot.Current(), requestedEndpoint);
        if (!connection.Succeeded)
        {
            RestoreEntryAfterFailure(generation, connection.Error!);
            return null;
        }
        var candidate = new SetupConnectionLease(connection.Client!);
        if (ReferenceEquals(candidate.Client, main.lifecycle.EntryRecoveryClient)) candidate.TransferToSession();
        try
        {
            ClientSetupResult available = await candidate.Client.ReadSetupAsync();
            if (IsCurrentSetupLoad(generation, requestedEndpoint))
            {
                if (available.Succeeded) return (candidate, available.Choices!);
                RestoreEntryAfterFailure(generation, available.Error!);
            }
        }
        catch
        {
            candidate.Dispose();
            throw;
        }
        candidate.Dispose();
        return null;
    }

    private bool InstallEntry(long generation, string requestedEndpoint, SetupConnectionLease candidate,
        ClientEntryResult result)
    {
        if (main.lifecycle.RetainEntry(generation, candidate.Client, result)) candidate.TransferToSession();
        if (!IsCurrentSetupLoad(generation, requestedEndpoint)) return false;
        if (!result.Succeeded)
        {
            RestoreEntryAfterFailure(generation, result.Error!);
            return false;
        }
        if (!main.lifecycle.Enter(generation, candidate.Client, result)) return false;
        candidate.TransferToSession();
        return true;
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
            var prepared = await PrepareEntryAsync(generation, requestedEndpoint);
            if (prepared is null) return;
            using SetupConnectionLease candidate = prepared.Value.Lease;
            SetupChoices choices = prepared.Value.Choices;
            ClientEntryResult attached = main.lifecycle.EntryRecovery is { } pending
                ? await candidate.Client.RetryEntryAsync(pending)
                : await candidate.Client.AttachAsync(main.gameId.Text, secret);
            secret = string.Empty;
            if (!InstallEntry(generation, requestedEndpoint, candidate, attached)) return;
            main.boardController.ResetForSession();
            main.currentProgress = null;
            main.transcript.Reset(
                seed: null,
                runtime: choices.Runtime,
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

        if (!InvitationClipboard.TryCopy(ref main.transientInvitation,
                DisplayServer.ClipboardSet, DisplayServer.ClipboardGet))
        {
            main.status.Text = "CLIPBOARD UNAVAILABLE  ·  YOUR INVITATION IS STILL AVAILABLE TO COPY";
            return;
        }
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
        if (!InteractionControl.IsUsable(main) || !main.lifecycle.IsCurrentLifetime(generation)) return;
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
