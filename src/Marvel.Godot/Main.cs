using System.Globalization;
using System.Text;
using Godot;
using Marvel.Client;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The desktop client's composition boundary and root scene.</summary>
public sealed partial class Main : Control
{
    private const double LastResultLifetimeSeconds = 8.0;
    private readonly List<string> scenarioNames = [];
    private readonly List<ScenarioSetupChoice> visibleModes = [];
    private readonly ICardArtProvider art = LocalArtPack.OpenConfigured();
    private readonly Dictionary<int, bool> expandedAreas = [];
    private Control board = null!;
    private VBoxContainer boardAreas = null!;
    private Label buildIdentity = null!;
    private BoardPresentation? boardPresentation;
    private HSplitContainer playLayout = null!;
    private BoardRenderResult? boardRender;
    private PanelContainer cardInspector = null!;
    private ScrollContainer cardInspectorScroll = null!;
    private VBoxContainer cardInspectorContent = null!;
    private int cardInspectorGeneration;
    private bool cardInspectorHovered;
    private bool cardInspectorPinned;
    private int? inspectedCardId;
    private Label briefingHero = null!;
    private Label briefingMode = null!;
    private Label briefingModular = null!;
    private Label briefingScenario = null!;
    private Label description = null!;
    private DecisionPanel decisions = null!;
    private PanelContainer activeResolution = null!;
    private Label activeResolutionSummary = null!;
    private Label eyebrow = null!;
    private PanelContainer eventCue = null!;
    private Label eventCueKind = null!;
    private Label eventCueSummary = null!;
    private RichTextLabel eventLog = null!;
    private CheckButton eventMotion = null!;
    private Button eventSkip = null!;
    private Button copyReport = null!;
    private Button saveReport = null!;
    private Tween? eventTween;
    private int eventGeneration;
    private readonly EventChronology events = new();
    private readonly InteractionTranscript transcript = new();
    private LineEdit endpoint = null!;
    private LineEdit gameId = null!;
    private OptionButton hero = null!;
    private PanelContainer invitationOffer = null!;
    private Button invitationCopy = null!;
    private LineEdit invitation = null!;
    private InterfaceScale interfaceScale = InterfaceScale.Standard;
    private HSlider interfaceScaleSlider = null!;
    private Label interfaceScaleValue = null!;
    private Button join = null!;
    private VBoxContainer joinFields = null!;
    private Button joinFlow = null!;
    private LocalGameClient? client;
    private ClientSession? session;
    private VBoxContainer contentStack = null!;
    private GameProgressPresentation? currentProgress;
    private OptionButton mode = null!;
    private MenuButton modular = null!;
    private ModularConfiguration modularConfiguration = ModularConfiguration.Recommended;
    private readonly HashSet<string> selectedModularKeys = new(StringComparer.Ordinal);
    private ScrollContainer pageScroll = null!;
    private PanelContainer promptPanel = null!;
    private VBoxContainer promptStack = null!;
    private Label promptContext = null!;
    private Label promptDiagnostic = null!;
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
    private Label syncStatus = null!;
    private HBoxContainer handRail = null!;
    private Label handHeading = null!;
    private PanelContainer lastResult = null!;
    private Button lastResultDismiss = null!;
    private Label lastResultSummary = null!;
    private Button lastResultToggle = null!;
    private int lastResultGeneration;
    private bool lastResultExpanded;
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
        interfaceScale = scale;
        Theme = ClientTheme.Create(scale);
        GetNode<ColorRect>("Table").Color = ClientTheme.ToGodot(VisualSystem.Palette.Canvas);
        GetNode<ColorRect>("TopRule").Color = ClientTheme.ToGodot(VisualSystem.Palette.Danger);
        GetNode<ColorRect>(
            "Margin/Shell/Content/Setup/Briefing/Frame/EncounterRail").Color =
            ClientTheme.ToGodot(VisualSystem.Palette.Danger);
        GetWindow().MinSize = new Vector2I(1040, 680);
        BindNodes();
        buildIdentity.Text = EngineBuildIdentity.Display;
        buildIdentity.TooltipText = $"Source commit {EngineBuildIdentity.Commit}";
        interfaceScaleSlider.SetValueNoSignal((double)scale);
        interfaceScaleSlider.ValueChanged += value =>
            ApplyInterfaceScale((InterfaceScale)(Mathf.RoundToInt(value / 10) * 10));
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
        modular.GetPopup().IdPressed += OnModularChoicePressed;
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

    /// <inheritdoc />
    public override void _ExitTree()
    {
        ReleaseEventTween();
        ClientComposition.Flush(TimeSpan.FromSeconds(3));
    }

    private void ApplyInterfaceScale(InterfaceScale scale)
    {
        interfaceScale = scale;
        Theme = ClientTheme.Create(scale);
        // The scale control is the ruler for the rest of the interface. Keep
        // its own geometry fixed so changing the value does not move the
        // pointer target beneath the user's hand.
        GetNode<Control>("StatusBar").Theme = ClientTheme.Create(InterfaceScale.Compact);
        interfaceScaleValue.Text = $"Scale {Mathf.RoundToInt(VisualSystem.ScalePercent(scale))}%";
        decisions.SetInterfaceScale(scale);
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
                     lastResultToggle, lastResultDismiss,
                 })
        {
            control.CustomMinimumSize = new Vector2(
                control.CustomMinimumSize.X,
                minimumHeight);
        }
        eventSkip.CustomMinimumSize = new Vector2(
            eventSkip.CustomMinimumSize.X,
            minimumHeight);
        if (CurrentGame?.World is { } world)
        {
            RenderBoard(world);
        }
        ApplyResponsivePlayLayout();
    }

    private void BindNodes()
    {
        const string content = "Margin/Shell/Content";
        pageScroll = GetNode<ScrollContainer>("Margin");
        contentStack = GetNode<VBoxContainer>($"{content}");
        description = GetNode<Label>($"{content}/Description");
        eyebrow = GetNode<Label>($"{content}/Eyebrow");
        title = GetNode<Label>($"{content}/Title");
        setupPanel = GetNode<Control>($"{content}/Setup");
        board = GetNode<Control>($"{content}/Play");
        playLayout = GetNode<HSplitContainer>($"{content}/Play");
        promptPanel = GetNode<PanelContainer>($"{content}/Play/Prompt");
        promptStack = GetNode<VBoxContainer>($"{content}/Play/Prompt/Margin/Stack");
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
        activeResolution = GetNode<PanelContainer>(
            $"{content}/Play/Prompt/Margin/Stack/ActiveResolution");
        activeResolutionSummary = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/ActiveResolution/Margin/Copy/Summary");
        promptDiagnostic = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/History/PromptDiagnostic");
        boardAreas = GetNode<VBoxContainer>($"{content}/Play/Board/TableScroll/Margin/Areas");
        handHeading = GetNode<Label>($"{content}/Play/Board/HandShelf/Margin/Stack/Heading");
        handRail = GetNode<HBoxContainer>(
            $"{content}/Play/Board/HandShelf/Margin/Stack/Scroll/Rail");
        decisions = GetNode<DecisionPanel>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/Action/Decision");
        lastResult = GetNode<PanelContainer>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/Action/LastResult");
        lastResultToggle = GetNode<Button>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/Action/LastResult/Margin/Copy/Header/Toggle");
        lastResultDismiss = GetNode<Button>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/Action/LastResult/Margin/Copy/Header/Dismiss");
        lastResultSummary = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/Action/LastResult/Margin/Copy/Summary");
        eventLog = GetNode<RichTextLabel>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/History/EventLog");
        eventCue = GetNode<PanelContainer>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/History/EventCue");
        eventCueKind = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/History/EventCue/Margin/Copy/Kind");
        eventCueSummary = GetNode<Label>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/History/EventCue/Margin/Copy/Summary");
        eventMotion = GetNode<CheckButton>("StatusBar/Motion");
        eventSkip = GetNode<Button>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/History/EventHeader/Skip");
        copyReport = GetNode<Button>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/History/EventHeader/CopyReport");
        saveReport = GetNode<Button>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/History/EventHeader/SaveReport");
        eventMotion.Toggled += enabled =>
        {
            if (!enabled)
            {
                SkipEventPresentation();
            }
        };
        eventSkip.Pressed += SkipEventPresentation;
        copyReport.Pressed += CopyInteractionReport;
        saveReport.Pressed += SaveInteractionReport;
        lastResultToggle.Pressed += ToggleLastResult;
        lastResultDismiss.Pressed += DismissLastResult;
        decisions.Submitted += OnDecisionSubmitted;
        decisions.DraftStarted += DismissLastResult;
        decisions.AnchorFocused += ids => boardRender?.Highlight(ids);
        decisions.CardHovered += PreviewHandCard;
        decisions.ProgressChanged += RenderDecisionProgress;
        cardInspector = GetNode<PanelContainer>("CardInspector");
        cardInspectorScroll = GetNode<ScrollContainer>("CardInspector/Scroll");
        cardInspectorContent = GetNode<VBoxContainer>("CardInspector/Scroll/Content");
        cardInspector.MouseEntered += () =>
        {
            cardInspectorHovered = true;
            cardInspectorGeneration++;
            cardInspector.FocusMode = FocusModeEnum.Click;
            cardInspectorScroll.FocusMode = FocusModeEnum.Click;
        };
        cardInspector.MouseExited += () =>
        {
            cardInspectorHovered = false;
            ScheduleCardInspectorHide();
        };
        BindCardInspectorFocus(cardInspector);
        BindCardInspectorFocus(cardInspectorScroll);
        interfaceScaleSlider = GetNode<HSlider>("StatusBar/InterfaceScale");
        interfaceScaleValue = GetNode<Label>("StatusBar/ScaleValue");
        buildIdentity = GetNode<Label>("StatusBar/BuildIdentity");
        syncStatus = GetNode<Label>("StatusBar/SyncStatus");
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
        modular = GetNode<MenuButton>($"{content}/Setup/Selections/Fields/Grid/Modular");
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
        synchronize = GetNode<Button>("StatusBar/Synchronize");
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
        title.ThemeTypeVariation = GodotThemeVariations.DisplayTitle;
        description.Visible = true;
        description.Text = joinMode
            ? "Connect to an already-running engine and use a one-time seat invitation."
            : "Choose an authored Core Set assignment. The engine validates it again when play starts.";
        RefreshEntryAvailability();
    }

    private void ApplyResponsivePlayLayout()
    {
        // This is a presentation choice: keep the prompt rail stable and give
        // the scrollable table every remaining pixel at desktop window sizes.
        DesktopPlayMetrics layout = VisualSystem.DesktopPlay(
            Math.Max(1, Mathf.RoundToInt(Size.X)),
            Math.Max(1, Mathf.RoundToInt(Size.Y)),
            interfaceScale);
        bool compactHeight = Size.Y < 800;
        promptPanel.CustomMinimumSize = new Vector2(layout.DecisionWidth, 0);
        setupGrid.Columns = Size.X >= 1500 ? 4 : 2;
        contentStack.ThemeTypeVariation = board.Visible && compactHeight
            ? GodotThemeVariations.TightStack
            : GodotThemeVariations.Stack;
        promptStack.ThemeTypeVariation = compactHeight
            ? GodotThemeVariations.TightStack
            : GodotThemeVariations.Stack;
        decisions.CustomMinimumSize = new Vector2(
            0,
            layout.DecisionMinimumHeight);
        eventCue.CustomMinimumSize = new Vector2(0, 68);
        eventLog.CustomMinimumSize = new Vector2(0, compactHeight ? 180 : 300);
        playLayout.SplitOffsets = [0];
        pageScroll.HorizontalScrollMode = board.Visible
            ? ScrollContainer.ScrollMode.Disabled
            : ScrollContainer.ScrollMode.Auto;
        pageScroll.FollowFocus = !board.Visible;
        pageScroll.VerticalScrollMode = board.Visible
            ? (int)interfaceScale <= 100
                ? ScrollContainer.ScrollMode.Disabled
                : ScrollContainer.ScrollMode.Auto
            : ScrollContainer.ScrollMode.Auto;
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

        ScenarioSetupChoice campaign = SelectedCampaign();
        string recommended = string.Join(
            ", ",
            campaign.RecommendedModularSets.Select(key =>
                setupChoices.ModularSets.Single(set => set.Key == key).Name));
        PopupMenu popup = modular.GetPopup();
        popup.Clear();
        popup.AddCheckItem($"Use recommended · {recommended}", 0);
        popup.AddCheckItem("No modular set", 1);
        popup.AddSeparator();
        for (int index = 0; index < setupChoices.ModularSets.Count; index++)
        {
            popup.AddCheckItem(setupChoices.ModularSets[index].Name, index + 2);
        }

        modularConfiguration = ModularConfiguration.Recommended;
        selectedModularKeys.Clear();
        RefreshModularControl();
    }

    private void OnModularChoicePressed(long id)
    {
        if (setupChoices is null)
        {
            return;
        }

        if (id == 0)
        {
            modularConfiguration = ModularConfiguration.Recommended;
            selectedModularKeys.Clear();
        }
        else if (id == 1)
        {
            modularConfiguration = ModularConfiguration.None;
            selectedModularKeys.Clear();
        }
        else if (id - 2 < setupChoices.ModularSets.Count)
        {
            string key = setupChoices.ModularSets[(int)id - 2].Key;
            if (!selectedModularKeys.Add(key))
            {
                selectedModularKeys.Remove(key);
            }

            modularConfiguration = selectedModularKeys.Count == 0
                ? ModularConfiguration.None
                : ModularConfiguration.Selected;
        }

        RefreshModularControl();
        RefreshBriefing();
    }

    private void RefreshModularControl()
    {
        PopupMenu popup = modular.GetPopup();
        for (int index = 0; index < popup.ItemCount; index++)
        {
            long id = popup.GetItemId(index);
            bool selected = id switch
            {
                0 => modularConfiguration == ModularConfiguration.Recommended,
                1 => modularConfiguration == ModularConfiguration.None,
                >= 2 when setupChoices is not null
                    && id - 2 < setupChoices.ModularSets.Count =>
                    selectedModularKeys.Contains(setupChoices.ModularSets[(int)id - 2].Key),
                _ => false,
            };
            if (popup.IsItemCheckable(index))
            {
                popup.SetItemChecked(index, selected);
            }
        }

        modular.Text = modularConfiguration switch
        {
            ModularConfiguration.Recommended => popup.GetItemText(0),
            ModularConfiguration.None => "No modular set",
            _ => string.Join(", ", setupChoices!.ModularSets
                .Where(set => selectedModularKeys.Contains(set.Key))
                .Select(set => set.Name)),
        };
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
        briefingModular.Text = modular.Text;
        RefreshStartAvailability();
    }

    private void RefreshStartAvailability()
    {
        bool validSeed = string.IsNullOrWhiteSpace(seed.Text) || uint.TryParse(
            seed.Text.Trim(),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out _);
        start.Disabled = setupChoices is null || !validSeed || gameId.Text.Length == 0;
        if (setupChoices is not null && !validSeed)
        {
            status.Text = "SEED INVALID  ·  ENTER 0 THROUGH 4294967295 OR LEAVE IT BLANK";
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

            if (string.IsNullOrWhiteSpace(seed.Text))
            {
                seed.Text = GameSeed.Create().ToString(CultureInfo.InvariantCulture);
            }

            GameSetupSelection selection = SelectedSetup();
            LocalClientConnection connection = ClientComposition.Connect(
                DesktopDataRoot.Current(),
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
            currentProgress = null;
            transcript.Reset(
                uint.Parse(seed.Text, CultureInfo.InvariantCulture),
                available.Choices!.Runtime);
            transientInvitation = startup.Invitations.Count == 0
                ? null
                : startup.Invitations[0].Invitation;
            invitationOffer.Visible = transientInvitation is not null;
            RenderGame(startup.Response!, resetEvents: true, operation: EngineProtocol.Open);
            setupPanel.Visible = false;
            board.Visible = true;
            eyebrow.Text = endpoint.Text.Length == 0
                ? $"CORE SET  /  EMBEDDED TABLE  /  SEED {seed.Text}"
                : $"CORE SET  /  HOSTED TABLE  /  SEED {seed.Text}";
            title.ThemeTypeVariation = GodotThemeVariations.BriefingTitle;
            ApplyResponsivePlayLayout();
            description.Visible = true;
            pageScroll.ScrollVertical = 0;
            pageScroll.SetDeferred("scroll_vertical", 0);
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
                DesktopDataRoot.Current(),
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
            ClientEntryResult attached = await client!.AttachAsync(gameId.Text, secret);
            secret = string.Empty;
            if (!attached.Succeeded)
            {
                RestoreEntryAfterFailure(attached.Error!);
                return;
            }

            session = attached.Session;
            currentProgress = null;
            transcript.Reset(seed: null, runtime: available.Choices!.Runtime);
            RenderGame(
                attached.Response!,
                resetEvents: true,
                operation: EngineProtocol.Attach);
            setupPanel.Visible = false;
            board.Visible = true;
            eyebrow.Text = "CORE SET  /  JOINED TABLE";
            title.ThemeTypeVariation = GodotThemeVariations.BriefingTitle;
            ApplyResponsivePlayLayout();
            description.Visible = true;
            pageScroll.ScrollVertical = 0;
            pageScroll.SetDeferred("scroll_vertical", 0);
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

    private void CopyInteractionReport()
    {
        DisplayServer.ClipboardSet(transcript.Export());
        status.Text = "INTERACTION REPORT COPIED  ·  AUTHORIZED GAME CONTENT INCLUDED";
    }

    private void SaveInteractionReport()
    {
        string path = Path.Combine(OS.GetUserDataDir(), "marvel-interaction-report.json");
        File.WriteAllText(path, transcript.Export());
        status.Text = $"INTERACTION REPORT SAVED  ·  {path}";
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
            transcript.RecordDecision(CurrentGame!.Revision, decision);
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
                synchronize.TooltipText = "Read the current authoritative table.";
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
        syncStatus.Text = "⚠ Sync needed";
        synchronize.TooltipText = "Reconnect to the current authoritative table.";
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
                RenderGame(
                    result.Response!,
                    preserveEvents: true,
                    priorProgress: prior,
                    operation: EngineProtocol.Sync);
                synchronize.TooltipText = "Read the current authoritative table.";
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
                prior.LocksDecisions,
                prior.OperationalLock));
        }
        syncStatus.Text = "⚠ Sync needed";
        synchronize.TooltipText = "Reconnect to the current authoritative table.";
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
        activeResolution.Visible = false;
        lastResult.Visible = false;
        lastResultGeneration++;
        RenderEvents();
        boardAreas.GetChildren().ToList().ForEach(node => node.QueueFree());
        board.Visible = false;
        setupPanel.Visible = true;
        decisionPending = false;
        resolveInFlight = false;
        uncertainMutationError = null;
        synchronize.TooltipText = "Read the current authoritative table.";
        synchronize.Disabled = true;
        synchronize.Visible = false;
        syncStatus.Visible = false;
        cardInspector.Visible = false;
        SetSetupControlsEnabled(true);
        ShowEntryMode(joinMode: true);
        ShowFailure(error);
    }

    private void RenderGame(
        EngineResponse response,
        bool resetEvents = false,
        bool preserveEvents = false,
        GameProgressPresentation? priorProgress = null,
        string operation = EngineProtocol.Resolve)
    {
        transcript.RecordResponse(operation, response);
        Outcome previousOutcome = CurrentGame?.World?.Outcome ?? Outcome.Unfinished;
        CurrentGame = response;
        WorldDescriptor world = response.World!;
        RenderBoard(world);
        syncStatus.Visible = true;
        syncStatus.Text = $"✓ Synced · r{response.Revision}";
        synchronize.Visible = true;
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
            RenderLastResult(presented.Highlights, resetEvents);
            PresentEvents(presented.Cues);
        }
        // A synchronized snapshot is authoritative but is not a new
        // transition, so it does not alter the diagnostic chronology.
        ApplyProgress(GameProgressPresentation.FromSynchronization(
            response,
            priorProgress ?? currentProgress));
        pageScroll.ScrollVertical = 0;
        pageScroll.SetDeferred("scroll_vertical", 0);
        RefreshSynchronizeAvailability();
        if (response.Prompt is null && world.Outcome != Outcome.Unfinished)
        {
            CallDeferred(MethodName.RevealOutcome);
        }
    }

    private void RenderBoard(WorldDescriptor world)
    {
        boardPresentation = BoardPresentation.From(world);
        boardRender = BoardRenderer.Render(
            boardAreas,
            boardPresentation,
            handRail,
            handHeading,
            interfaceScale,
            expandedAreas,
            art);
        boardRender.CardActivated += (card, control) => ToggleCardInspector(card, control);
        HideCardInspector();
    }

    private void PreviewHandCard(int? id)
    {
        if (cardInspectorPinned)
        {
            return;
        }

        if (id is null)
        {
            HideCardInspector();
            return;
        }

        BoardCardPresentation? card = boardPresentation?.Areas
            .Where(area => area.Zone == "HandsArea")
            .SelectMany(area => area.Cards)
            .FirstOrDefault(candidate => candidate.TargetId == id);
        Control? source = boardRender?.ControlFor(id.Value);
        if (card is null || source is null)
        {
            return;
        }

        ShowCardInspector(card, source, pinned: false);
    }

    private void ToggleCardInspector(BoardCardPresentation card, Control? source)
    {
        if (card.Concealed)
        {
            return;
        }

        if (cardInspector.Visible && inspectedCardId == card.TargetId)
        {
            HideCardInspector();
            return;
        }

        ShowCardInspector(card, source, pinned: true);
    }

    private void ShowCardInspector(
        BoardCardPresentation card, Control? source, bool pinned)
    {
        cardInspectorGeneration++;
        inspectedCardId = card.TargetId;
        Control? priorFocus = GetViewport()?.GuiGetFocusOwner();

        foreach (Node child in cardInspectorContent.GetChildren())
        {
            cardInspectorContent.RemoveChild(child);
            child.QueueFree();
        }

        CardControl detail = CardControl.Create(
            card, CardDisplaySize.Full, interfaceScale, art);
        IgnoreMouseRecursively(detail);
        cardInspectorContent.AddChild(detail);
        float width = detail.CustomMinimumSize.X + 24;
        float height = Math.Min(Size.Y - 48, detail.CustomMinimumSize.Y + 24);
        Rect2 sourceRect = source?.GetGlobalRect() ?? new Rect2(
            GetViewport().GetMousePosition(), Vector2.Zero);
        if (!pinned)
        {
            height = Math.Min(height, Math.Max(160, sourceRect.Position.Y - 24));
        }
        cardInspector.CustomMinimumSize = Vector2.Zero;
        cardInspector.Size = new Vector2(width, Math.Max(pinned ? 240 : 160, height));
        Vector2 anchor = sourceRect.Position + sourceRect.Size / 2;
        FloatingPanelPosition position = pinned
            ? VisualSystem.PlaceFloatingPanel(
                Mathf.RoundToInt(Size.X),
                Mathf.RoundToInt(Size.Y),
                Mathf.RoundToInt(anchor.X),
                Mathf.RoundToInt(anchor.Y),
                Mathf.RoundToInt(width),
                Mathf.RoundToInt(height))
            : new FloatingPanelPosition(
                Mathf.RoundToInt(Mathf.Clamp(
                    anchor.X - width / 2, 12, Math.Max(12, Size.X - width - 12))),
                Mathf.RoundToInt(Mathf.Max(12, sourceRect.Position.Y - height - 12)));
        cardInspector.Position = new Vector2(position.X, position.Y);
        cardInspectorPinned = pinned;
        cardInspector.Visible = true;
        if (priorFocus is not null && !cardInspector.IsAncestorOf(priorFocus))
        {
            Callable.From(priorFocus.GrabFocus).CallDeferred();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (cardInspector.Visible
            && @event is InputEventMouseButton
            {
                ButtonIndex: MouseButton.Left,
                Pressed: true,
            } click
            && !cardInspector.GetGlobalRect().HasPoint(click.Position)
            && !IsInsideCard(GetViewport()?.GuiGetHoveredControl()))
        {
            HideCardInspector();
        }
    }

    private static bool IsInsideCard(Node? node)
    {
        for (Node? current = node; current is not null; current = current.GetParent())
        {
            if (current is CardControl)
            {
                return true;
            }
        }

        return false;
    }

    private void ScheduleCardInspectorHide()
    {
        int generation = ++cardInspectorGeneration;
        GetTree().CreateTimer(0.3).Timeout += () =>
        {
            if (generation == cardInspectorGeneration
                && !cardInspectorPinned
                && !cardInspectorHovered
                && !CardInspectorHasFocus())
            {
                cardInspector.FocusMode = FocusModeEnum.None;
                cardInspectorScroll.FocusMode = FocusModeEnum.None;
                inspectedCardId = null;
                cardInspector.Visible = false;
            }
        };
    }

    private void BindCardInspectorFocus(Control control)
    {
        control.FocusEntered += () => cardInspectorGeneration++;
        control.FocusExited += ScheduleCardInspectorHide;
    }

    private bool CardInspectorHasFocus()
    {
        Control? focused = GetViewport()?.GuiGetFocusOwner();
        return focused is not null
            && (focused == cardInspector || cardInspector.IsAncestorOf(focused));
    }

    private void HideCardInspector()
    {
        cardInspectorGeneration++;
        cardInspectorPinned = false;
        cardInspectorHovered = false;
        cardInspector.FocusMode = FocusModeEnum.None;
        cardInspectorScroll.FocusMode = FocusModeEnum.None;
        inspectedCardId = null;
        cardInspector.Visible = false;
    }

    private static void IgnoreMouseRecursively(Node node)
    {
        if (node is Control control)
        {
            control.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
        foreach (Node child in node.GetChildren())
        {
            IgnoreMouseRecursively(child);
        }
    }

    private void RevealOutcome()
    {
        pageScroll.ScrollVertical = 0;
        pageScroll.SetDeferred("scroll_vertical", 0);
    }

    private void RenderPromptSummary(Prompt? prompt, WorldDescriptor world)
    {
        if (prompt is null)
        {
            activeResolution.Visible = false;
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
            promptDiagnostic.Text = "No prompt is pending.";
            return;
        }

        PromptPresentation view = PromptPresentation.From(prompt, world);
        promptEyebrow.Text = "CURRENT DECISION";
        promptHeading.Text = view.Heading;
        promptContext.Text = view.Context;
        activeResolution.Visible = !string.IsNullOrWhiteSpace(view.Resolution);
        activeResolutionSummary.Text = view.Resolution;
        promptRequirement.Text = view.Requirement;
        promptDiagnostic.Text = view.Diagnostic;
        promptRequirement.ThemeTypeVariation = prompt.Cancellable
            ? GodotThemeVariations.Caption
            : GodotThemeVariations.DangerText;
    }

    private void RenderLastResult(
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

        int generation = ++lastResultGeneration;
        lastResult.Visible = true;
        lastResultSummary.Text = string.Join(" ", highlights.Select(entry => entry.Summary));
        lastResult.ThemeTypeVariation = highlights.Any(entry => entry.Motion is
            EventMotionKind.Defeat or EventMotionKind.Terminal)
                ? GodotThemeVariations.DangerStatusPanel
                : GodotThemeVariations.StatusPanel;
        SetLastResultExpanded(true);
        GetTree().CreateTimer(LastResultLifetimeSeconds).Timeout += () =>
        {
            if (generation == lastResultGeneration && IsInsideTree())
            {
                DismissLastResult();
            }
        };
    }

    private void ToggleLastResult()
    {
        if (lastResult.Visible)
        {
            SetLastResultExpanded(!lastResultExpanded);
        }
    }

    private void SetLastResultExpanded(bool expanded)
    {
        lastResultExpanded = expanded;
        lastResultSummary.Visible = expanded;
        lastResultToggle.Text = expanded ? "Collapse" : "Expand";
    }

    private void DismissLastResult()
    {
        lastResultGeneration++;
        lastResultExpanded = false;
        lastResult.Visible = false;
        lastResultSummary.Visible = false;
        lastResultSummary.Text = string.Empty;
        lastResultToggle.Text = "Expand";
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
            eventTween.TweenCallback(
                Callable.From(() => BeginEventCue(entry, generation))).Dispose();
            eventTween.TweenProperty(eventCue, "modulate:a", 1.0f, 0.10).Dispose();
            eventTween.TweenInterval(0.30).Dispose();
            eventTween.TweenProperty(eventCue, "modulate:a", 0.35f, 0.10).Dispose();
        }

        eventTween.TweenCallback(
            Callable.From(() => FinishEventPresentation(generation))).Dispose();
    }

    private void BeginEventCue(EventPresentation entry, int generation)
    {
        if (generation != eventGeneration)
        {
            return;
        }

        eventCueKind.Text = entry.Motion.ToString().ToUpperInvariant();
        eventCue.Visible = true;
        eventCueSummary.Text = entry.Summary;
        eventCueKind.ThemeTypeVariation = entry.Motion switch
        {
            EventMotionKind.Damage or EventMotionKind.Defeat or EventMotionKind.Terminal =>
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
        ReleaseEventTween();
        SetEventPresentationSettled();
    }

    private void ReleaseEventTween()
    {
        Tween? tween = eventTween;
        eventTween = null;
        if (tween is null)
        {
            return;
        }

        tween.Kill();
        tween.Dispose();
    }

    private void FinishEventPresentation(int generation)
    {
        if (generation != eventGeneration)
        {
            return;
        }

        SetEventPresentationSettled();
    }

    private void SetEventPresentationSettled()
    {
        eventCue.Visible = false;
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
            or GameProgressKind.Unavailable
            or GameProgressKind.ServiceUnavailable
            or GameProgressKind.VersionMismatch
            or GameProgressKind.SessionUnavailable
            or GameProgressKind.StorageFailure;
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
        var heroes = new List<string> { setupChoices!.Heroes[hero.Selected].Key };
        if (secondHero.Selected > 0)
        {
            heroes.Add(secondHero.GetItemMetadata(secondHero.Selected).AsString());
        }

        return new GameSetupSelection(
            heroes,
            SelectedCampaign().Key,
            modularConfiguration,
            setupChoices.ModularSets
                .Where(set => selectedModularKeys.Contains(set.Key))
                .Select(set => set.Key)
                .ToArray(),
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
