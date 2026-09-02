using System.Globalization;
using System.Text;
using Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The desktop client's composition boundary and root scene.</summary>
public sealed partial class Main : Control
{
    private readonly List<string> scenarioNames = [];
    private readonly List<ScenarioSetupChoice> visibleModes = [];
    private readonly ICardArtProvider art = LocalArtPack.OpenConfigured();
    private Control board = null!;
    private VBoxContainer boardAreas = null!;
    private HSplitContainer playLayout = null!;
    private BoardRenderResult? boardRender;
    private Label briefingHero = null!;
    private Label briefingMode = null!;
    private Label briefingModular = null!;
    private Label briefingScenario = null!;
    private Label description = null!;
    private DecisionPanel decisions = null!;
    private Label eyebrow = null!;
    private PanelContainer eventCue = null!;
    private Label eventCueKind = null!;
    private Label eventCueSummary = null!;
    private RichTextLabel eventLog = null!;
    private CheckButton eventMotion = null!;
    private Button eventSkip = null!;
    private Tween? eventTween;
    private int eventGeneration;
    private readonly EventChronology events = new();
    private LineEdit endpoint = null!;
    private LineEdit gameId = null!;
    private OptionButton hero = null!;
    private PanelContainer invitationOffer = null!;
    private Button invitationCopy = null!;
    private LineEdit invitation = null!;
    private Button join = null!;
    private VBoxContainer joinFields = null!;
    private Button joinFlow = null!;
    private LocalGameClient? client;
    private ClientSession? session;
    private GameProgressPresentation? currentProgress;
    private OptionButton mode = null!;
    private OptionButton modular = null!;
    private ScrollContainer pageScroll = null!;
    private PanelContainer promptPanel = null!;
    private Label promptContext = null!;
    private Label promptEyebrow = null!;
    private Label promptHeading = null!;
    private Label promptProgress = null!;
    private Label promptRequirement = null!;
    private Button reloadSetup = null!;
    private OptionButton scenario = null!;
    private OptionButton secondHero = null!;
    private LineEdit seed = null!;
    private GridContainer setupGrid = null!;
    private Label setupHeading = null!;
    private Label seedHelp = null!;
    private Control setupPanel = null!;
    private SetupChoices? setupChoices;
    private Button start = null!;
    private Button startFlow = null!;
    private Label status = null!;
    private PanelContainer statusPanel = null!;
    private Button synchronize = null!;
    private Label title = null!;
    private string? transientInvitation;
    private bool decisionPending;
    private bool resolveInFlight;
    private bool joining;
    private int setupLoadGeneration;
    private bool setupLoading;
    private bool synchronizing;
    private ClientStartupError? uncertainMutationError;

    /// <summary>The latest complete visibility-safe response accepted as authoritative.</summary>
    public EngineResponse? CurrentGame { get; private set; }

    /// <inheritdoc />
    public override void _Ready()
    {
        InterfaceScale scale = ClientTheme.ConfiguredScale();
        Theme = ClientTheme.Create(scale);
        GetNode<ColorRect>("Table").Color = ClientTheme.ToGodot(VisualSystem.Palette.Canvas);
        GetNode<ColorRect>("TopRule").Color = ClientTheme.ToGodot(VisualSystem.Palette.Danger);
        GetNode<ColorRect>(
            "Margin/Shell/Content/Setup/Briefing/Frame/EncounterRail").Color =
            ClientTheme.ToGodot(VisualSystem.Palette.Danger);
        GetWindow().MinSize = new Vector2I(1040, 680);
        BindNodes();
        ApplyInterfaceScale(scale);
        Resized += ApplyResponsivePlayLayout;
        hero.ItemSelected += _ =>
        {
            PopulateSecondHeroChoices();
            RefreshBriefing();
        };
        secondHero.ItemSelected += _ => RefreshBriefing();
        scenario.ItemSelected += OnScenarioSelected;
        mode.ItemSelected += _ =>
        {
            PopulateModularChoices();
            RefreshBriefing();
        };
        modular.ItemSelected += _ => RefreshBriefing();
        seed.TextChanged += _ => RefreshStartAvailability();
        endpoint.TextChanged += _ => OnEndpointChanged();
        gameId.TextChanged += _ => RefreshEntryAvailability();
        invitation.TextChanged += _ => RefreshEntryAvailability();
        startFlow.Pressed += () => ShowEntryMode(joinMode: false);
        joinFlow.Pressed += () => ShowEntryMode(joinMode: true);
        start.Pressed += OnStartPressed;
        join.Pressed += OnJoinPressed;
        invitationCopy.Pressed += CopyInvitation;
        synchronize.Pressed += OnSynchronizePressed;
        reloadSetup.Pressed += () => _ = LoadSetupAsync();
        endpoint.Text = OS.GetEnvironment("MARVEL_ENGINE_ENDPOINT");
        _ = LoadSetupAsync();
    }

    private void ApplyInterfaceScale(InterfaceScale scale)
    {
        float minimumHeight = VisualSystem.Controls(scale).MinimumHeight;
        foreach (Control control in new Control[]
                 {
                     endpoint, gameId, hero, secondHero, scenario, mode, modular, seed,
                     invitation,
                 })
        {
            control.CustomMinimumSize = new Vector2(
                control.CustomMinimumSize.X,
                minimumHeight);
        }

        foreach (Control control in new Control[]
                 {
                     startFlow, joinFlow, reloadSetup, start, join, invitationCopy,
                     synchronize,
                 })
        {
            control.CustomMinimumSize = new Vector2(
                control.CustomMinimumSize.X,
                Math.Max(control.CustomMinimumSize.Y, minimumHeight));
        }
        foreach (Control control in new Control[] { eventMotion, eventSkip })
        {
            control.CustomMinimumSize = new Vector2(
                control.CustomMinimumSize.X,
                minimumHeight);
        }
    }

    private void BindNodes()
    {
        const string content = "Margin/Shell/Content";
        pageScroll = GetNode<ScrollContainer>("Margin");
        description = GetNode<Label>($"{content}/Description");
        eyebrow = GetNode<Label>($"{content}/Eyebrow");
        title = GetNode<Label>($"{content}/Title");
        setupPanel = GetNode<Control>($"{content}/Setup");
        board = GetNode<Control>($"{content}/Play");
        playLayout = GetNode<HSplitContainer>($"{content}/Play");
        promptPanel = GetNode<PanelContainer>($"{content}/Play/Prompt");
        promptEyebrow = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/PromptHeader/Eyebrow");
        promptHeading = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/PromptHeader/Heading");
        promptContext = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/PromptHeader/Context");
        promptRequirement = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/PromptHeader/Requirement");
        promptProgress = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/PromptHeader/Progress");
        boardAreas = GetNode<VBoxContainer>($"{content}/Play/Board/Margin/Areas");
        decisions = GetNode<DecisionPanel>(
            $"{content}/Play/Prompt/Margin/Stack/DecisionScroll/Decision");
        eventLog = GetNode<RichTextLabel>(
            $"{content}/Play/Prompt/Margin/Stack/EventLog");
        eventCue = GetNode<PanelContainer>(
            $"{content}/Play/Prompt/Margin/Stack/EventCue");
        eventCueKind = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/EventCue/Margin/Copy/Kind");
        eventCueSummary = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/EventCue/Margin/Copy/Summary");
        eventMotion = GetNode<CheckButton>(
            $"{content}/Play/Prompt/Margin/Stack/EventHeader/Motion");
        eventSkip = GetNode<Button>(
            $"{content}/Play/Prompt/Margin/Stack/EventHeader/Skip");
        eventMotion.Toggled += enabled =>
        {
            if (!enabled)
            {
                SkipEventPresentation();
            }
        };
        eventSkip.Pressed += SkipEventPresentation;
        decisions.Submitted += OnDecisionSubmitted;
        decisions.AnchorFocused += ids => boardRender?.Highlight(ids);
        decisions.ProgressChanged += RenderDecisionProgress;
        endpoint = GetNode<LineEdit>(
            $"{content}/Setup/Selections/Fields/ConnectionGrid/Endpoint");
        gameId = GetNode<LineEdit>(
            $"{content}/Setup/Selections/Fields/ConnectionGrid/GameId");
        startFlow = GetNode<Button>(
            $"{content}/Setup/Selections/Fields/EntryModes/StartFlow");
        joinFlow = GetNode<Button>(
            $"{content}/Setup/Selections/Fields/EntryModes/JoinFlow");
        setupHeading = GetNode<Label>($"{content}/Setup/Selections/Fields/Heading");
        reloadSetup = GetNode<Button>(
            $"{content}/Setup/Selections/Fields/ReloadSetup");
        setupGrid = GetNode<GridContainer>($"{content}/Setup/Selections/Fields/Grid");
        hero = GetNode<OptionButton>($"{content}/Setup/Selections/Fields/Grid/Hero");
        secondHero = GetNode<OptionButton>(
            $"{content}/Setup/Selections/Fields/Grid/SecondHero");
        scenario = GetNode<OptionButton>($"{content}/Setup/Selections/Fields/Grid/Scenario");
        mode = GetNode<OptionButton>($"{content}/Setup/Selections/Fields/Grid/Mode");
        modular = GetNode<OptionButton>($"{content}/Setup/Selections/Fields/Grid/Modular");
        seed = GetNode<LineEdit>($"{content}/Setup/Selections/Fields/Grid/Seed");
        seedHelp = GetNode<Label>($"{content}/Setup/Selections/Fields/SeedHelp");
        start = GetNode<Button>($"{content}/Setup/Selections/Fields/Start");
        joinFields = GetNode<VBoxContainer>(
            $"{content}/Setup/Selections/Fields/JoinFields");
        invitation = GetNode<LineEdit>(
            $"{content}/Setup/Selections/Fields/JoinFields/Invitation");
        join = GetNode<Button>($"{content}/Setup/Selections/Fields/JoinFields/Join");
        invitationOffer = GetNode<PanelContainer>(
            $"{content}/Play/Prompt/Margin/Stack/InvitationOffer");
        invitationCopy = GetNode<Button>(
            $"{content}/Play/Prompt/Margin/Stack/InvitationOffer/Margin/Row/CopyInvitation");
        synchronize = GetNode<Button>(
            $"{content}/Play/Prompt/Margin/Stack/Synchronize");
        briefingScenario = GetNode<Label>(
            $"{content}/Setup/Briefing/Frame/Copy/Scenario");
        briefingMode = GetNode<Label>($"{content}/Setup/Briefing/Frame/Copy/Mode");
        briefingHero = GetNode<Label>($"{content}/Setup/Briefing/Frame/Copy/Hero");
        briefingModular = GetNode<Label>(
            $"{content}/Setup/Briefing/Frame/Copy/Modular");
        status = GetNode<Label>($"{content}/Status/Text");
        statusPanel = GetNode<PanelContainer>($"{content}/Status");
        ShowEntryMode(joinMode: false);
        CallDeferred(MethodName.ApplyResponsivePlayLayout);
    }

    private void ShowEntryMode(bool joinMode)
    {
        joining = joinMode;
        setupHeading.Visible = !joinMode;
        reloadSetup.Visible = !joinMode;
        setupGrid.Visible = !joinMode;
        seedHelp.Visible = !joinMode;
        start.Visible = !joinMode;
        joinFields.Visible = joinMode;
        GetNode<Control>("Margin/Shell/Content/Setup/Briefing").Visible = !joinMode;
        startFlow.Disabled = false;
        joinFlow.Disabled = false;
        startFlow.ThemeTypeVariation = joinMode
            ? string.Empty
            : GodotThemeVariations.PrimaryButton;
        joinFlow.ThemeTypeVariation = joinMode
            ? GodotThemeVariations.PrimaryButton
            : string.Empty;
        eyebrow.Text = joinMode
            ? "CORE SET  /  JOIN TABLE"
            : "CORE SET  /  MISSION BRIEFING";
        description.Text = joinMode
            ? "Connect to an already-running engine and use a one-time seat invitation."
            : "Choose an authored Core Set assignment. The engine validates it again when play starts.";
        RefreshEntryAvailability();
    }

    private void ApplyResponsivePlayLayout()
    {
        // This is a presentation choice: keep the prompt rail stable and give
        // the scrollable table every remaining pixel at desktop window sizes.
        float promptWidth = Math.Clamp(Size.X * 0.30f, 330f, 440f);
        promptPanel.CustomMinimumSize = new Vector2(promptWidth, 0);
        playLayout.SplitOffsets = [0];
    }

    private async Task LoadSetupAsync()
    {
        int generation = ++setupLoadGeneration;
        string requestedEndpoint = endpoint.Text;
        setupLoading = true;
        setupChoices = null;
        client = null;
        reloadSetup.Disabled = true;
        start.Disabled = true;
        SetAssignmentControlsEnabled(false);
        status.Text = "LOADING ASSIGNMENTS  ·  WAITING FOR ENGINE";
        try
        {
            LocalClientConnection connection = ClientComposition.Connect(
                ProjectSettings.GlobalizePath("res://../.."),
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

            client = candidate;
            setupChoices = setup.Choices;
            PopulateSetupChoices();
            setupLoading = false;
            reloadSetup.Disabled = false;
            title.Text = "Assemble the table.";
            description.Text =
                "Choose an authored Core Set assignment. The engine validates it again when play starts.";
            status.Text = "ASSIGNMENT READY  ·  CHOOSE A HERO AND ENCOUNTER";
            statusPanel.ThemeTypeVariation = GodotThemeVariations.StatusPanel;
            status.ThemeTypeVariation = GodotThemeVariations.StatusText;
        }
        catch (Exception)
        {
            ApplySetupFailure(generation, requestedEndpoint, new ClientStartupError(
                "startup_failed",
                "The setup options could not be loaded. Check the endpoint and try again."));
        }
    }

    private bool IsCurrentSetupLoad(int generation, string requestedEndpoint) =>
        generation == setupLoadGeneration
        && endpoint.Text == requestedEndpoint;

    private void ApplySetupFailure(
        int generation,
        string requestedEndpoint,
        ClientStartupError error)
    {
        if (!IsCurrentSetupLoad(generation, requestedEndpoint))
        {
            return;
        }

        setupLoading = false;
        reloadSetup.Disabled = false;
        ShowFailure(error);
    }

    private void OnEndpointChanged()
    {
        setupLoadGeneration++;
        setupLoading = false;
        setupChoices = null;
        SetAssignmentControlsEnabled(false);
        reloadSetup.Disabled = false;
        status.Text = "ENDPOINT CHANGED  ·  RELOAD SETUP OPTIONS";
        RefreshEntryAvailability();
    }

    private void PopulateSetupChoices()
    {
        hero.Clear();
        foreach (HeroSetupChoice choice in setupChoices!.Heroes)
        {
            hero.AddItem(choice.Name);
        }
        hero.Select(0);
        PopulateSecondHeroChoices();

        scenarioNames.Clear();
        scenarioNames.AddRange(
            setupChoices.Scenarios.Select(choice => choice.Name)
                .Distinct(StringComparer.Ordinal));
        scenario.Clear();
        foreach (string name in scenarioNames)
        {
            scenario.AddItem(name);
        }

        SetAssignmentControlsEnabled(true);
        OnScenarioSelected(0);
    }

    private void PopulateSecondHeroChoices()
    {
        secondHero.Clear();
        secondHero.AddItem("Solo table · one hero");
        if (setupChoices is null || setupChoices.Heroes.Count == 0)
        {
            return;
        }

        int primary = Math.Max(hero.Selected, 0);
        string primaryKey = setupChoices.Heroes[primary].Key;
        foreach (HeroSetupChoice choice in setupChoices.Heroes.Where(
                     choice => choice.Key != primaryKey))
        {
            secondHero.AddItem(choice.Name);
            secondHero.SetItemMetadata(secondHero.ItemCount - 1, choice.Key);
        }

        secondHero.Select(0);
    }

    private void OnScenarioSelected(long selected)
    {
        if (setupChoices is null || selected < 0 || selected >= scenarioNames.Count)
        {
            return;
        }

        visibleModes.Clear();
        visibleModes.AddRange(setupChoices.Scenarios.Where(choice =>
            choice.Name == scenarioNames[(int)selected]));
        mode.Clear();
        foreach (ScenarioSetupChoice choice in visibleModes)
        {
            mode.AddItem(choice.Expert ? "Expert" : "Standard");
        }

        mode.Select(0);
        PopulateModularChoices();
        RefreshBriefing();
    }

    private void PopulateModularChoices()
    {
        if (setupChoices is null || visibleModes.Count == 0)
        {
            return;
        }

        modular.Clear();
        ScenarioSetupChoice campaign = SelectedCampaign();
        string recommended = string.Join(
            ", ",
            campaign.RecommendedModularSets.Select(key =>
                setupChoices.ModularSets.Single(set => set.Key == key).Name));
        modular.AddItem($"Recommended · {recommended}");
        modular.AddItem("No modular set");
        foreach (ModularSetupChoice choice in setupChoices.ModularSets)
        {
            modular.AddItem(choice.Name);
        }

        modular.Select(0);
    }

    private void RefreshBriefing()
    {
        if (setupChoices is null || visibleModes.Count == 0)
        {
            return;
        }

        ScenarioSetupChoice campaign = SelectedCampaign();
        briefingScenario.Text = campaign.Name;
        briefingMode.Text = campaign.Expert ? "EXPERT MODE" : "STANDARD MODE";
        briefingHero.Text = secondHero.Selected <= 0
            ? setupChoices.Heroes[hero.Selected].Name
            : $"{setupChoices.Heroes[hero.Selected].Name} + {secondHero.GetItemText(secondHero.Selected)}";
        briefingModular.Text = modular.GetItemText(modular.Selected);
        RefreshStartAvailability();
    }

    private void RefreshStartAvailability()
    {
        bool validSeed = uint.TryParse(
            seed.Text,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out _);
        start.Disabled = setupChoices is null || !validSeed || gameId.Text.Length == 0;
        if (setupChoices is not null && !validSeed)
        {
            status.Text = "SEED REQUIRED  ·  ENTER A WHOLE NUMBER FROM 0 THROUGH 4294967295";
        }
        else if (setupChoices is not null && CurrentGame is null)
        {
            status.Text = "ASSIGNMENT READY  ·  START WHEN THE TABLE IS SET";
        }
    }

    private void RefreshEntryAvailability()
    {
        RefreshStartAvailability();
        join.Disabled = endpoint.Text.Length == 0
            || gameId.Text.Length == 0
            || invitation.Text.Length == 0;
        if (joining && endpoint.Text.Length == 0)
        {
            status.Text = "REMOTE ENDPOINT REQUIRED  ·  JOIN AN ALREADY-RUNNING ENGINE";
        }
        else if (joining && gameId.Text.Length == 0)
        {
            status.Text = "GAME LABEL REQUIRED  ·  USE THE LABEL SHARED BY THE HOST";
        }
        else if (joining && invitation.Text.Length == 0)
        {
            status.Text = "INVITATION REQUIRED  ·  PASTE THE ONE-TIME SEAT SECRET";
        }
        else if (joining)
        {
            status.Text = "INVITATION READY  ·  JOIN WHEN THE ENDPOINT AND LABEL MATCH";
        }
    }

    private async void OnStartPressed()
    {
        try
        {
            start.Disabled = true;
            SetSetupControlsEnabled(false);
            status.Text = "DEALING GAME  ·  ONE MOMENT";

            GameSetupSelection selection = SelectedSetup();
            LocalClientConnection connection = ClientComposition.Connect(
                ProjectSettings.GlobalizePath("res://../.."),
                endpoint.Text);
            if (!connection.Succeeded)
            {
                RestoreEntryAfterFailure(connection.Error!);
                return;
            }

            client = connection.Client;
            ClientSetupResult available = await client!.ReadSetupAsync();
            if (!available.Succeeded)
            {
                RestoreEntryAfterFailure(available.Error!);
                return;
            }

            ClientEntryResult startup = await client.OpenSessionAsync(
                gameId.Text, available.Choices!, selection);
            if (!startup.Succeeded)
            {
                RestoreEntryAfterFailure(startup.Error!);
                return;
            }

            session = startup.Session;
            transientInvitation = startup.Invitations.Count == 0
                ? null
                : startup.Invitations[0].Invitation;
            invitationOffer.Visible = transientInvitation is not null;
            RenderGame(startup.Response!, resetEvents: true);
            setupPanel.Visible = false;
            board.Visible = true;
            eyebrow.Text = endpoint.Text.Length == 0
                ? "CORE SET  /  EMBEDDED TABLE"
                : "CORE SET  /  HOSTED TABLE";
        }
        catch (Exception)
        {
            SetSetupControlsEnabled(true);
            RefreshStartAvailability();
            ShowFailure(new ClientStartupError(
                "startup_failed",
                "The selected game could not be displayed. Check the assignment and try again."));
        }
    }

    private async void OnJoinPressed()
    {
        if (join.Disabled)
        {
            return;
        }

        string secret = invitation.Text;
        invitation.Clear();
        try
        {
            join.Disabled = true;
            SetSetupControlsEnabled(false);
            status.Text = "JOINING GAME  ·  ONE MOMENT";
            LocalClientConnection connection = ClientComposition.Connect(
                ProjectSettings.GlobalizePath("res://../.."),
                endpoint.Text);
            if (!connection.Succeeded)
            {
                RestoreEntryAfterFailure(connection.Error!);
                return;
            }

            client = connection.Client;
            ClientEntryResult attached = await client!.AttachAsync(gameId.Text, secret);
            secret = string.Empty;
            if (!attached.Succeeded)
            {
                RestoreEntryAfterFailure(attached.Error!);
                return;
            }

            session = attached.Session;
            RenderGame(attached.Response!, resetEvents: true);
            setupPanel.Visible = false;
            board.Visible = true;
            eyebrow.Text = "CORE SET  /  JOINED TABLE";
        }
        catch (Exception)
        {
            secret = string.Empty;
            RestoreEntryAfterFailure(new ClientStartupError(
                "startup_failed",
                "The invitation could not be attached. Check the endpoint and ask the host for a new invitation."));
        }
    }

    private void CopyInvitation()
    {
        if (transientInvitation is null)
        {
            return;
        }

        DisplayServer.ClipboardSet(transientInvitation);
        transientInvitation = null;
        invitationOffer.Visible = false;
        status.Text = "INVITATION COPIED  ·  THE ONE-TIME SECRET WAS REMOVED FROM THIS SCREEN";
    }

    private void RestoreEntryAfterFailure(ClientStartupError error)
    {
        SetSetupControlsEnabled(true);
        RefreshEntryAvailability();
        ShowFailure(error);
    }

    private async void OnDecisionSubmitted(EngineDecision decision)
    {
        if (decisionPending || resolveInFlight)
        {
            return;
        }

        resolveInFlight = true;
        RefreshSynchronizeAvailability();
        try
        {
            ApplyProgress(GameProgressPresentation.Resolving());
            promptProgress.Text = "RESOLVING  ·  WAITING FOR ENGINE";
            promptProgress.ThemeTypeVariation = GodotThemeVariations.StatusText;
            ClientResolutionResult result = await client!.ResolveAsync(
                session!, decision);
            if (!IsInsideTree())
            {
                return;
            }

            if (result.SessionDisposition == ClientSessionDisposition.Unavailable)
            {
                ReturnToJoinAfterSessionLoss(result.Error ?? new ClientStartupError(
                    "session_unavailable",
                    "This table session is no longer available. Join again with a new invitation."));
                return;
            }

            if (result.MutationDisposition == ClientMutationDisposition.NotSent)
            {
                decisionPending = false;
                uncertainMutationError = null;
                ApplyProgress(GameProgressPresentation.DecisionNotSent(
                    result.Error ?? new ClientStartupError(
                        "decision_not_sent",
                        "The decision did not reach the game service.")));
                promptProgress.Text = "NOT SENT  ·  RETRY SAFE";
                promptProgress.ThemeTypeVariation = GodotThemeVariations.StatusText;
                return;
            }

            if (result.HasAuthoritativeView)
            {
                RenderGame(result.Response!);
                decisionPending = false;
                uncertainMutationError = null;
                if (result.Error is not null)
                {
                    ApplyProgress(GameProgressPresentation.Recovered(
                        result.Response!, result.Error));
                }
                return;
            }

            ClientStartupError failure = result.Error ?? new ClientStartupError(
                "decision_unresolved",
                "The decision result could not be reconciled with the current table.");
            if (result.MutationDisposition == ClientMutationDisposition.Rejected)
            {
                decisionPending = false;
                uncertainMutationError = null;
                ApplyProgress(GameProgressPresentation.DecisionRejected(failure));
                promptProgress.Text = "REJECTED  ·  SYNCHRONIZE TABLE";
                promptProgress.ThemeTypeVariation = GodotThemeVariations.DangerText;
                synchronize.Text = "Synchronize table";
            }
            else
            {
                decisionPending = true;
                uncertainMutationError = failure;
                ShowUnconfirmed(failure);
            }
        }
        catch (Exception)
        {
            if (IsInsideTree())
            {
                decisionPending = true;
                uncertainMutationError = new ClientStartupError(
                    "display_failed",
                    "The decision result could not be displayed.");
                ShowUnconfirmed(uncertainMutationError);
            }
        }
        finally
        {
            resolveInFlight = false;
            if (IsInsideTree())
            {
                RefreshSynchronizeAvailability();
            }
        }
    }

    private void ShowUnconfirmed(ClientStartupError error)
    {
        ApplyProgress(GameProgressPresentation.Unconfirmed(error));
        promptProgress.Text =
            $"UNCONFIRMED  ·  {error.Code.ToUpperInvariant()}  ·  RESTART OR RECONNECT";
        promptProgress.ThemeTypeVariation = GodotThemeVariations.DangerText;
        synchronize.Text = "Reconnect table";
    }

    private async void OnSynchronizePressed()
    {
        if (synchronizing || resolveInFlight || client is null || session is null)
        {
            return;
        }

        GameProgressPresentation prior = currentProgress
            ?? GameProgressPresentation.FromResponse(CurrentGame!);
        bool hadUncertainMutation = decisionPending;
        synchronizing = true;
        RefreshSynchronizeAvailability();
        ApplyProgress(GameProgressPresentation.Synchronizing());
        try
        {
            ClientSynchronizationResult result = await client.SynchronizeAsync(session);
            if (!IsInsideTree())
            {
                return;
            }

            if (result.Succeeded)
            {
                decisionPending = false;
                uncertainMutationError = null;
                RenderGame(result.Response!, preserveEvents: true);
                synchronize.Text = "Synchronize table";
            }
            else if (result.SessionDisposition == ClientSessionDisposition.Unavailable)
            {
                ReturnToJoinAfterSessionLoss(result.Error!);
            }
            else
            {
                ApplySynchronizationFailure(
                    result.Error!, prior, hadUncertainMutation);
            }
        }
        catch (Exception)
        {
            if (IsInsideTree())
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
            synchronizing = false;
            if (IsInsideTree())
            {
                RefreshSynchronizeAvailability();
            }
        }
    }

    private void ApplySynchronizationFailure(
        ClientStartupError error,
        GameProgressPresentation prior,
        bool hadUncertainMutation)
    {
        if (hadUncertainMutation)
        {
            decisionPending = true;
            ShowUnconfirmed(uncertainMutationError ?? error);
        }
        else
        {
            ApplyProgress(GameProgressPresentation.SynchronizationUnavailable(
                error,
                prior.LocksDecisions));
        }
        synchronize.Text = "Reconnect table";
    }

    private void ReturnToJoinAfterSessionLoss(ClientStartupError error)
    {
        session = null;
        client = null;
        CurrentGame = null;
        transientInvitation = null;
        invitation.Clear();
        invitationOffer.Visible = false;
        boardRender = null;
        events.Reset([]);
        RenderEvents();
        boardAreas.GetChildren().ToList().ForEach(node => node.QueueFree());
        board.Visible = false;
        setupPanel.Visible = true;
        decisionPending = false;
        resolveInFlight = false;
        uncertainMutationError = null;
        synchronize.Text = "Synchronize table";
        synchronize.Disabled = true;
        SetSetupControlsEnabled(true);
        ShowEntryMode(joinMode: true);
        ShowFailure(error);
    }

    private void RenderGame(
        EngineResponse response,
        bool resetEvents = false,
        bool preserveEvents = false)
    {
        Outcome previousOutcome = CurrentGame?.World?.Outcome ?? Outcome.Unfinished;
        CurrentGame = response;
        WorldDescriptor world = response.World!;
        boardRender = BoardRenderer.Render(boardAreas, BoardPresentation.From(world), art);
        RenderPromptSummary(response.Prompt, world);
        decisions.Render(response.Prompt, world);
        if (!preserveEvents)
        {
            EventBatchPresentation presented = EventCuePlanner.Plan(
                response.Events,
                world,
                previousOutcome);
            if (resetEvents)
            {
                events.Reset(presented.History);
            }
            else
            {
                events.Append(presented.History);
            }

            RenderEvents();
            PresentEvents(presented.Cues);
        }
        // A synchronized snapshot is authoritative but is not a new
        // transition, so it does not alter the diagnostic chronology.
        ApplyProgress(GameProgressPresentation.FromResponse(response));
        RefreshSynchronizeAvailability();
        if (response.Prompt is null && world.Outcome != Outcome.Unfinished)
        {
            CallDeferred(MethodName.RevealOutcome);
        }
    }

    private void RevealOutcome() => pageScroll.ScrollVertical = 0;

    private void RenderPromptSummary(Prompt? prompt, WorldDescriptor world)
    {
        if (prompt is null)
        {
            (promptEyebrow.Text, promptHeading.Text, promptContext.Text) =
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
            promptRequirement.Text = world.Outcome == Outcome.Unfinished
                ? "WAITING"
                : "RESOLVED";
            promptRequirement.ThemeTypeVariation = GodotThemeVariations.StatusText;
            promptProgress.Text = "NO INPUT PENDING";
            return;
        }

        PromptPresentation view = PromptPresentation.From(prompt, world);
        promptEyebrow.Text = "CURRENT DECISION";
        promptHeading.Text = view.Heading;
        promptContext.Text = view.Context;
        promptRequirement.Text = view.Requirement;
        promptRequirement.ThemeTypeVariation = prompt.Cancellable
            ? GodotThemeVariations.Caption
            : GodotThemeVariations.DangerText;
    }

    private void RenderDecisionProgress(DecisionProgressPresentation? progress)
    {
        if (progress is null)
        {
            promptProgress.Text = "NO INPUT PENDING";
            promptProgress.ThemeTypeVariation = GodotThemeVariations.StatusText;
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
        promptProgress.Text = $"{targets}  ·  {payment}  ·  "
            + (progress.IsReady ? "READY" : "INCOMPLETE");
        promptProgress.ThemeTypeVariation = progress.IsReady
            ? GodotThemeVariations.StatusText
            : GodotThemeVariations.Caption;
    }

    private void RenderEvents()
    {
        if (events.Entries.Count == 0)
        {
            eventLog.Text = "No events yet.";
            return;
        }

        string accent = ClientTheme.ToGodot(VisualSystem.Palette.Accent).ToHtml(false);
        string muted = ClientTheme.ToGodot(VisualSystem.Palette.MutedText).ToHtml(false);
        var text = new StringBuilder();
        for (int index = 0; index < events.Entries.Count; index++)
        {
            EventPresentation entry = events.Entries[index];
            text.Append("[color=#")
                .Append(accent)
                .Append(']')
                .Append((index + 1).ToString("000", CultureInfo.InvariantCulture))
                .Append("[/color]  ")
                .AppendLine(entry.Summary)
                .Append("     [color=#")
                .Append(muted)
                .Append(']')
                .Append(entry.Cause)
                .AppendLine("[/color]");
        }

        eventLog.Text = text.ToString();
        eventLog.ScrollToLine(eventLog.GetLineCount());
    }

    private void PresentEvents(IReadOnlyList<EventPresentation> presented)
    {
        SkipEventPresentation();
        if (!eventMotion.ButtonPressed || presented.Count == 0)
        {
            return;
        }

        eventSkip.Disabled = false;
        int generation = eventGeneration;
        eventTween = CreateTween();
        foreach (EventPresentation entry in presented)
        {
            eventTween.TweenCallback(Callable.From(() => BeginEventCue(entry, generation)));
            eventTween.TweenProperty(eventCue, "modulate:a", 1.0f, 0.10);
            eventTween.TweenInterval(0.30);
            eventTween.TweenProperty(eventCue, "modulate:a", 0.35f, 0.10);
        }

        eventTween.TweenCallback(Callable.From(() => FinishEventPresentation(generation)));
    }

    private void BeginEventCue(EventPresentation entry, int generation)
    {
        if (generation != eventGeneration)
        {
            return;
        }

        eventCueKind.Text = entry.Motion.ToString().ToUpperInvariant();
        eventCueSummary.Text = entry.Summary;
        eventCueKind.ThemeTypeVariation = entry.Motion switch
        {
            EventMotionKind.Damage or EventMotionKind.Terminal =>
                GodotThemeVariations.DangerText,
            EventMotionKind.Create or EventMotionKind.Heal =>
                GodotThemeVariations.StatusText,
            _ => GodotThemeVariations.Eyebrow,
        };
        eventCue.Modulate = new Color(1f, 1f, 1f, 0.20f);
        boardRender?.Present(entry.Anchors);
    }

    private void SkipEventPresentation()
    {
        eventGeneration++;
        eventTween?.Kill();
        eventTween = null;
        SetEventPresentationSettled();
    }

    private void FinishEventPresentation(int generation)
    {
        if (generation != eventGeneration)
        {
            return;
        }

        eventTween = null;
        SetEventPresentationSettled();
    }

    private void SetEventPresentationSettled()
    {
        eventCueKind.Text = "TABLE SYNCED";
        eventCueKind.ThemeTypeVariation = GodotThemeVariations.StatusText;
        eventCueSummary.Text = "The authoritative board is current.";
        eventCue.Modulate = Colors.White;
        eventSkip.Disabled = true;
        boardRender?.Present([]);
    }

    private void ApplyProgress(GameProgressPresentation progress)
    {
        currentProgress = progress;
        title.Text = progress.Title;
        description.Text = progress.Description;
        status.Text = progress.Status;
        bool danger = progress.Kind is GameProgressKind.VillainWins
            or GameProgressKind.PlayersLose
            or GameProgressKind.DecisionRejected
            or GameProgressKind.SynchronizationUnavailable
            or GameProgressKind.Unconfirmed
            or GameProgressKind.Unavailable;
        statusPanel.ThemeTypeVariation = danger
            ? GodotThemeVariations.DangerStatusPanel
            : GodotThemeVariations.StatusPanel;
        status.ThemeTypeVariation = danger
            ? GodotThemeVariations.DangerText
            : GodotThemeVariations.StatusText;
        decisions.SetSubmitting(progress.LocksDecisions);
    }

    private void RefreshSynchronizeAvailability() =>
        synchronize.Disabled = session is null || synchronizing || resolveInFlight;

    private GameSetupSelection SelectedSetup()
    {
        ModularConfiguration configuration = modular.Selected switch
        {
            0 => ModularConfiguration.Recommended,
            1 => ModularConfiguration.None,
            _ => ModularConfiguration.Selected,
        };
        string? modularKey = modular.Selected >= 2
            ? setupChoices!.ModularSets[modular.Selected - 2].Key
            : null;
        var heroes = new List<string> { setupChoices!.Heroes[hero.Selected].Key };
        if (secondHero.Selected > 0)
        {
            heroes.Add(secondHero.GetItemMetadata(secondHero.Selected).AsString());
        }

        return new GameSetupSelection(
            heroes,
            SelectedCampaign().Key,
            configuration,
            modularKey,
            seed.Text);
    }

    private ScenarioSetupChoice SelectedCampaign() => visibleModes[mode.Selected];

    private void SetSetupControlsEnabled(bool enabled)
    {
        SetAssignmentControlsEnabled(enabled);
        endpoint.Editable = enabled;
        gameId.Editable = enabled;
        invitation.Editable = enabled;
        startFlow.Disabled = !enabled;
        joinFlow.Disabled = !enabled;
        reloadSetup.Disabled = !enabled || setupLoading;
    }

    private void SetAssignmentControlsEnabled(bool enabled)
    {
        hero.Disabled = !enabled;
        secondHero.Disabled = !enabled;
        scenario.Disabled = !enabled;
        mode.Disabled = !enabled;
        modular.Disabled = !enabled;
        seed.Editable = enabled;
    }

    private void ShowFailure(ClientStartupError failure)
    {
        ApplyProgress(GameProgressPresentation.Unavailable(failure));
    }
}
