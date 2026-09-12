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

    internal MainSetupController(Main main)
    {
        this.main = main;
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
        main.status.Text = "LOADING ASSIGNMENTS  ·  WAITING FOR ENGINE";
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
            main.description.Text =
                "Choose an authored Core Set assignment. The engine validates it again when play starts.";
            main.status.Text = "ASSIGNMENT READY  ·  CHOOSE A HERO AND ENCOUNTER";
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
        main.status.Text = "ENDPOINT CHANGED  ·  RELOAD SETUP OPTIONS";
        RefreshEntryAvailability();
    }

    internal void PopulateSetupChoices()
    {
        main.hero.Clear();
        foreach (HeroSetupChoice choice in main.setupChoices!.Heroes)
        {
            main.hero.AddItem(choice.Name);
        }
        main.hero.Select(0);
        PopulateSecondHeroChoices();

        main.scenarioNames.Clear();
        main.scenarioNames.AddRange(
            main.setupChoices.Scenarios.Select(choice => choice.Name)
                .Distinct(StringComparer.Ordinal));
        main.scenario.Clear();
        foreach (string name in main.scenarioNames)
        {
            main.scenario.AddItem(name);
        }

        main.SetAssignmentControlsEnabled(true);
        OnScenarioSelected(0);
    }

    internal void PopulateSecondHeroChoices()
    {
        main.secondHero.Clear();
        main.secondHero.AddItem("Solo table · one hero");
        if (main.setupChoices is null || main.setupChoices.Heroes.Count == 0)
        {
            return;
        }

        int primary = Math.Max(main.hero.Selected, 0);
        string primaryKey = main.setupChoices.Heroes[primary].Key;
        foreach (HeroSetupChoice choice in main.setupChoices.Heroes.Where(
                     choice => choice.Key != primaryKey))
        {
            main.secondHero.AddItem(choice.Name);
            main.secondHero.SetItemMetadata(main.secondHero.ItemCount - 1, choice.Key);
        }

        main.secondHero.Select(0);
    }

    internal void OnScenarioSelected(long selected)
    {
        if (main.setupChoices is null || selected < 0 || selected >= main.scenarioNames.Count)
        {
            return;
        }

        main.visibleModes.Clear();
        main.visibleModes.AddRange(main.setupChoices.Scenarios.Where(choice =>
            choice.Name == main.scenarioNames[(int)selected]));
        main.mode.Clear();
        foreach (ScenarioSetupChoice choice in main.visibleModes)
        {
            main.mode.AddItem(choice.Expert ? "Expert" : "Standard");
        }

        main.mode.Select(0);
        PopulateModularChoices();
        RefreshBriefing();
    }

    internal void PopulateModularChoices()
    {
        if (main.setupChoices is null || main.visibleModes.Count == 0)
        {
            return;
        }

        ScenarioSetupChoice campaign = main.SelectedCampaign();
        string recommended = string.Join(
            ", ",
            campaign.RecommendedModularSets.Select(key =>
                main.setupChoices.ModularSets.Single(set => set.Key == key).Name));
        PopupMenu popup = main.modular.GetPopup();
        popup.Clear();
        popup.AddCheckItem($"Use recommended · {recommended}", 0);
        popup.AddCheckItem("No modular set", 1);
        popup.AddSeparator();
        for (int index = 0; index < main.setupChoices.ModularSets.Count; index++)
        {
            popup.AddCheckItem(main.setupChoices.ModularSets[index].Name, index + 2);
        }

        main.modularConfiguration = ModularConfiguration.Recommended;
        main.selectedModularKeys.Clear();
        RefreshModularControl();
    }

    internal void OnModularChoicePressed(long id)
    {
        if (main.setupChoices is null)
        {
            return;
        }

        if (id == 0)
        {
            main.modularConfiguration = ModularConfiguration.Recommended;
            main.selectedModularKeys.Clear();
        }
        else if (id == 1)
        {
            main.modularConfiguration = ModularConfiguration.None;
            main.selectedModularKeys.Clear();
        }
        else if (id - 2 < main.setupChoices.ModularSets.Count)
        {
            string key = main.setupChoices.ModularSets[(int)id - 2].Key;
            if (!main.selectedModularKeys.Add(key))
            {
                main.selectedModularKeys.Remove(key);
            }

            main.modularConfiguration = main.selectedModularKeys.Count == 0
                ? ModularConfiguration.None
                : ModularConfiguration.Selected;
        }

        RefreshModularControl();
        RefreshBriefing();
    }

    internal void RefreshModularControl()
    {
        PopupMenu popup = main.modular.GetPopup();
        for (int index = 0; index < popup.ItemCount; index++)
        {
            long id = popup.GetItemId(index);
            bool selected = id switch
            {
                0 => main.modularConfiguration == ModularConfiguration.Recommended,
                1 => main.modularConfiguration == ModularConfiguration.None,
                >= 2 when main.setupChoices is not null
                    && id - 2 < main.setupChoices.ModularSets.Count =>
                    main.selectedModularKeys.Contains(main.setupChoices.ModularSets[(int)id - 2].Key),
                _ => false,
            };
            if (popup.IsItemCheckable(index))
            {
                popup.SetItemChecked(index, selected);
            }
        }

        main.modular.Text = main.modularConfiguration switch
        {
            ModularConfiguration.Recommended => popup.GetItemText(0),
            ModularConfiguration.None => "No modular set",
            _ => string.Join(", ", main.setupChoices!.ModularSets
                .Where(set => main.selectedModularKeys.Contains(set.Key))
                .Select(set => set.Name)),
        };
    }

    internal void RefreshBriefing()
    {
        if (main.setupChoices is null || main.visibleModes.Count == 0)
        {
            return;
        }

        ScenarioSetupChoice campaign = main.SelectedCampaign();
        main.briefingScenario.Text = campaign.Name;
        main.briefingMode.Text = campaign.Expert ? "EXPERT MODE" : "STANDARD MODE";
        main.briefingHero.Text = main.secondHero.Selected <= 0
            ? main.setupChoices.Heroes[main.hero.Selected].Name
            : $"{main.setupChoices.Heroes[main.hero.Selected].Name} + {main.secondHero.GetItemText(main.secondHero.Selected)}";
        main.briefingModular.Text = main.modular.Text;
        RefreshStartAvailability();
    }

    internal void RefreshStartAvailability()
    {
        bool validSeed = string.IsNullOrWhiteSpace(main.seed.Text) || uint.TryParse(
            main.seed.Text.Trim(),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out _);
        main.start.Disabled = main.setupChoices is null || !validSeed || main.gameId.Text.Length == 0;
        if (main.setupChoices is not null && !validSeed)
        {
            main.status.Text = "SEED INVALID  ·  ENTER 0 THROUGH 4294967295 OR LEAVE IT BLANK";
        }
        else if (main.setupChoices is not null && main.CurrentGame is null)
        {
            main.status.Text = "ASSIGNMENT READY  ·  START WHEN THE TABLE IS SET";
        }
    }

    internal void RefreshEntryAvailability()
    {
        RefreshStartAvailability();
        main.join.Disabled = main.endpoint.Text.Length == 0
            || main.gameId.Text.Length == 0
            || main.invitation.Text.Length == 0;
        if (main.joining && main.endpoint.Text.Length == 0)
        {
            main.status.Text = "REMOTE ENDPOINT REQUIRED  ·  JOIN AN ALREADY-RUNNING ENGINE";
        }
        else if (main.joining && main.gameId.Text.Length == 0)
        {
            main.status.Text = "GAME LABEL REQUIRED  ·  USE THE LABEL SHARED BY THE HOST";
        }
        else if (main.joining && main.invitation.Text.Length == 0)
        {
            main.status.Text = "INVITATION REQUIRED  ·  PASTE THE ONE-TIME SEAT SECRET";
        }
        else if (main.joining)
        {
            main.status.Text = "INVITATION READY  ·  JOIN WHEN THE ENDPOINT AND LABEL MATCH";
        }
    }

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
            main.RenderGame(startup.Response!, resetEvents: true, operation: EngineProtocol.Open);
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
            main.RenderGame(
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
