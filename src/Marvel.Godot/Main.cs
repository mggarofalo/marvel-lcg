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
    internal const double LastResultLifetimeSeconds = 8.0;
    internal readonly List<string> scenarioNames = [];
    internal readonly List<ScenarioSetupChoice> visibleModes = [];
    internal readonly ICardArtProvider art = LocalArtPack.OpenConfigured();
    internal readonly Dictionary<int, bool> expandedAreas = [];
    internal Control board = null!;
    internal VBoxContainer boardAreas = null!;
    internal Label buildIdentity = null!;
    internal BoardPresentation? boardPresentation;
    internal HSplitContainer playLayout = null!;
    internal BoardRenderResult? boardRender;
    internal Control cardInspector = null!;
    internal ColorRect cardInspectorBackdrop = null!;
    internal PanelContainer cardInspectorFrame = null!;
    internal Button cardInspectorClose = null!;
    internal Label cardInspectorTitle = null!;
    internal ScrollContainer cardInspectorScroll = null!;
    internal VBoxContainer cardInspectorContent = null!;
    internal Control? cardInspectorReturnFocus;
    internal int cardInspectorGeneration;
    internal bool cardInspectorHovered;
    internal bool cardInspectorPinned;
    internal int? inspectedCardId;
    internal Label briefingHero = null!;
    internal Label briefingMode = null!;
    internal Label briefingModular = null!;
    internal Label briefingScenario = null!;
    internal Label description = null!;
    internal DecisionPanel decisions = null!;
    internal PanelContainer activeResolution = null!;
    internal Label activeResolutionSummary = null!;
    internal Label eyebrow = null!;
    internal PanelContainer eventCue = null!;
    internal Label eventCueKind = null!;
    internal Label eventCueSummary = null!;
    internal RichTextLabel eventLog = null!;
    internal CheckButton eventMotion = null!;
    internal Button eventSkip = null!;
    internal Button undoLast = null!;
    internal Button copyReport = null!;
    internal Button saveReport = null!;
    internal Tween? eventTween;
    internal int eventGeneration;
    internal readonly EventChronology events = new();
    internal readonly InteractionTranscript transcript = new();
    internal LineEdit endpoint = null!;
    internal LineEdit gameId = null!;
    internal OptionButton hero = null!;
    internal PanelContainer invitationOffer = null!;
    internal Button invitationCopy = null!;
    internal LineEdit invitation = null!;
    internal InterfaceScale interfaceScale = InterfaceScale.Standard;
    internal HSlider interfaceScaleSlider = null!;
    internal Label interfaceScaleValue = null!;
    internal Button join = null!;
    internal VBoxContainer joinFields = null!;
    internal Button joinFlow = null!;
    internal MainSetupController setupController = null!;
    internal MainSessionController sessionController = null!;
    internal MainBoardController boardController = null!;
    internal MainEventController eventController = null!;
    internal MainLayoutController layoutController = null!;
    internal LocalGameClient? client;
    internal ClientSession? session;
    internal VBoxContainer contentStack = null!;
    internal GameProgressPresentation? currentProgress;
    internal OptionButton mode = null!;
    internal MenuButton modular = null!;
    internal ModularConfiguration modularConfiguration = ModularConfiguration.Recommended;
    internal readonly HashSet<string> selectedModularKeys = new(StringComparer.Ordinal);
    internal ScrollContainer pageScroll = null!;
    internal PanelContainer promptPanel = null!;
    internal VBoxContainer promptStack = null!;
    internal Label promptContext = null!;
    internal Label promptDiagnostic = null!;
    internal Label promptEyebrow = null!;
    internal Label promptHeading = null!;
    internal Label promptProgress = null!;
    internal Label promptRequirement = null!;
    internal Button reloadSetup = null!;
    internal OptionButton scenario = null!;
    internal OptionButton secondHero = null!;
    internal LineEdit seed = null!;
    internal GridContainer setupGrid = null!;
    internal Label setupHeading = null!;
    internal Label seedHelp = null!;
    internal Control setupPanel = null!;
    internal SetupChoices? setupChoices;
    internal Button start = null!;
    internal Button startFlow = null!;
    internal Label status = null!;
    internal PanelContainer statusPanel = null!;
    internal Button synchronize = null!;
    internal Label syncStatus = null!;
    internal HBoxContainer handRail = null!;
    internal Label handHeading = null!;
    internal PanelContainer lastResult = null!;
    internal Button lastResultDismiss = null!;
    internal Label lastResultSummary = null!;
    internal Button lastResultToggle = null!;
    internal int lastResultGeneration;
    internal bool lastResultExpanded;
    internal Label title = null!;
    internal string? transientInvitation;
    internal bool decisionPending;
    internal bool resolveInFlight;
    internal bool joining;
    internal int setupLoadGeneration;
    internal bool setupLoading;
    internal bool synchronizing;
    internal ClientStartupError? uncertainMutationError;

    /// <summary>The latest complete visibility-safe response accepted as authoritative.</summary>
    public EngineResponse? CurrentGame { get; internal set; }

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
        layoutController = new MainLayoutController(this);
        setupController = new MainSetupController(this);
        sessionController = new MainSessionController(this);
        boardController = new MainBoardController(this);
        eventController = new MainEventController(this);
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

    internal void ApplyInterfaceScale(InterfaceScale scale) =>
        layoutController.ApplyInterfaceScale(scale);

    internal void BindNodes()
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
        undoLast = GetNode<Button>(
            $"{content}/Play/Prompt/Margin/Stack/Workbench/History/EventHeader/UndoLast");
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
        undoLast.Pressed += OnUndoLastPressed;
        eventLog.MetaClicked += OnHistoryMetaClicked;
        copyReport.Pressed += CopyInteractionReport;
        saveReport.Pressed += SaveInteractionReport;
        lastResultToggle.Pressed += ToggleLastResult;
        lastResultDismiss.Pressed += DismissLastResult;
        decisions.Submitted += OnDecisionSubmitted;
        decisions.DraftStarted += DismissLastResult;
        decisions.AnchorFocused += ids => boardRender?.Highlight(ids);
        decisions.CardHovered += PreviewHandCard;
        decisions.ProgressChanged += RenderDecisionProgress;
        cardInspector = GetNode<Control>("CardInspector");
        cardInspectorBackdrop = GetNode<ColorRect>("CardInspector/Backdrop");
        cardInspectorFrame = GetNode<PanelContainer>("CardInspector/Frame");
        cardInspectorClose = GetNode<Button>("CardInspector/Frame/Stack/Header/Close");
        cardInspectorTitle = GetNode<Label>("CardInspector/Frame/Stack/Header/Title");
        cardInspectorScroll = GetNode<ScrollContainer>("CardInspector/Frame/Stack/Scroll");
        cardInspectorContent = GetNode<VBoxContainer>(
            "CardInspector/Frame/Stack/Scroll/Content");
        cardInspectorFrame.MouseEntered += () =>
        {
            cardInspectorHovered = true;
            cardInspectorGeneration++;
            cardInspectorFrame.FocusMode = FocusModeEnum.Click;
            cardInspectorScroll.FocusMode = FocusModeEnum.Click;
        };
        cardInspectorFrame.MouseExited += () =>
        {
            cardInspectorHovered = false;
            ScheduleCardInspectorHide();
        };
        cardInspectorClose.Pressed += HideCardInspector;
        BindCardInspectorFocus(cardInspectorFrame);
        BindCardInspectorFocus(cardInspectorClose);
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

    internal void ShowEntryMode(bool joinMode) => layoutController.ShowEntryMode(joinMode);
    internal void ApplyResponsivePlayLayout() => layoutController.ApplyResponsivePlayLayout();

    internal Task LoadSetupAsync() => setupController.LoadSetupAsync();

    internal bool IsCurrentSetupLoad(int generation, string requestedEndpoint) =>
        setupController.IsCurrentSetupLoad(generation, requestedEndpoint);

    internal void ApplySetupFailure(
        int generation,
        string requestedEndpoint,
        ClientStartupError error) =>
        setupController.ApplySetupFailure(generation, requestedEndpoint, error);

    internal void OnEndpointChanged() => setupController.OnEndpointChanged();
    internal void PopulateSetupChoices() => setupController.PopulateSetupChoices();
    internal void PopulateSecondHeroChoices() => setupController.PopulateSecondHeroChoices();
    internal void OnScenarioSelected(long selected) => setupController.OnScenarioSelected(selected);
    internal void PopulateModularChoices() => setupController.PopulateModularChoices();
    internal void OnModularChoicePressed(long id) => setupController.OnModularChoicePressed(id);
    internal void RefreshModularControl() => setupController.RefreshModularControl();
    internal void RefreshBriefing() => setupController.RefreshBriefing();
    internal void RefreshStartAvailability() => setupController.RefreshStartAvailability();
    internal void RefreshEntryAvailability() => setupController.RefreshEntryAvailability();
    internal void OnStartPressed() => setupController.OnStartPressed();
    internal void OnJoinPressed() => setupController.OnJoinPressed();
    internal void CopyInvitation() => setupController.CopyInvitation();
    internal void CopyInteractionReport() => setupController.CopyInteractionReport();
    internal void SaveInteractionReport() => setupController.SaveInteractionReport();
    internal void RestoreEntryAfterFailure(ClientStartupError error) =>
        setupController.RestoreEntryAfterFailure(error);
    internal void OnDecisionSubmitted(EngineDecision decision) =>
        sessionController.OnDecisionSubmitted(decision);
    internal void OnUndoLastPressed() => sessionController.OnUndoLastPressed();
    internal void OnHistoryMetaClicked(Variant meta) => sessionController.OnHistoryMetaClicked(meta);
    internal void UndoTo(int cursor) => sessionController.UndoTo(cursor);
    internal void ShowUnconfirmed(ClientStartupError error) => sessionController.ShowUnconfirmed(error);
    internal void OnSynchronizePressed() => sessionController.OnSynchronizePressed();
    internal void ApplySynchronizationFailure(
        ClientStartupError error,
        GameProgressPresentation prior,
        bool hadUncertainMutation) =>
        sessionController.ApplySynchronizationFailure(error, prior, hadUncertainMutation);
    internal void ReturnToJoinAfterSessionLoss(ClientStartupError error) =>
        sessionController.ReturnToJoinAfterSessionLoss(error);
    internal void RenderGame(
        EngineResponse response,
        bool resetEvents = false,
        bool preserveEvents = false,
        GameProgressPresentation? priorProgress = null,
        string operation = EngineProtocol.Resolve) =>
        boardController.RenderGame(response, resetEvents, preserveEvents, priorProgress, operation);
    internal void RenderBoard(WorldDescriptor world) => boardController.RenderBoard(world);
    internal void PreviewHandCard(int? id) => boardController.PreviewHandCard(id);
    internal void ToggleCardInspector(BoardCardPresentation card, Control? source) =>
        boardController.ToggleCardInspector(card, source);
    internal void ShowCardInspector(BoardCardPresentation card, Control? source, bool pinned) =>
        boardController.ShowCardInspector(card, source, pinned);
    public override void _Input(InputEvent @event) => boardController.Input(@event);
    internal static InterfaceScale FittedInspectionScale(
        BoardCardPresentation card,
        InterfaceScale requested,
        float viewportHeight) =>
        MainBoardController.FittedInspectionScale(card, requested, viewportHeight);
    internal static bool IsInsideCard(Node? node) => MainBoardController.IsInsideCard(node);
    internal void ScheduleCardInspectorHide() => boardController.ScheduleCardInspectorHide();
    internal void BindCardInspectorFocus(Control control) => boardController.BindCardInspectorFocus(control);
    internal bool CardInspectorHasFocus() => boardController.CardInspectorHasFocus();
    internal void HideCardInspector() => boardController.HideCardInspector();
    internal static void IgnoreMouseRecursively(Node node) =>
        MainBoardController.IgnoreMouseRecursively(node);
    internal void RevealOutcome() => eventController.RevealOutcome();
    internal void RenderPromptSummary(Prompt? prompt, WorldDescriptor world) =>
        eventController.RenderPromptSummary(prompt, world);
    internal void RenderLastResult(
        IReadOnlyList<EventPresentation> highlights,
        bool reset) =>
        eventController.RenderLastResult(highlights, reset);
    internal void ToggleLastResult() => eventController.ToggleLastResult();
    internal void SetLastResultExpanded(bool expanded) => eventController.SetLastResultExpanded(expanded);
    internal void DismissLastResult() => eventController.DismissLastResult();
    internal void RenderDecisionProgress(DecisionProgressPresentation? progress) =>
        eventController.RenderDecisionProgress(progress);
    internal void RenderEvents() => eventController.RenderEvents();
    internal void PresentEvents(IReadOnlyList<EventPresentation> presented) =>
        eventController.PresentEvents(presented);
    internal void BeginEventCue(EventPresentation entry, int generation) =>
        eventController.BeginEventCue(entry, generation);
    internal void SkipEventPresentation() => eventController.SkipEventPresentation();
    internal void ReleaseEventTween() => eventController.ReleaseEventTween();
    internal void FinishEventPresentation(int generation) => eventController.FinishEventPresentation(generation);
    internal void SetEventPresentationSettled() => eventController.SetEventPresentationSettled();
    internal void ApplyProgress(GameProgressPresentation progress) => eventController.ApplyProgress(progress);
    internal void RefreshSynchronizeAvailability() => eventController.RefreshSynchronizeAvailability();
    internal GameSetupSelection SelectedSetup()
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

    internal ScenarioSetupChoice SelectedCampaign() => visibleModes[mode.Selected];

    internal void SetSetupControlsEnabled(bool enabled)
    {
        SetAssignmentControlsEnabled(enabled);
        endpoint.Editable = enabled;
        gameId.Editable = enabled;
        invitation.Editable = enabled;
        startFlow.Disabled = !enabled;
        joinFlow.Disabled = !enabled;
        reloadSetup.Disabled = !enabled || setupLoading;
    }

    internal void SetAssignmentControlsEnabled(bool enabled)
    {
        hero.Disabled = !enabled;
        secondHero.Disabled = !enabled;
        scenario.Disabled = !enabled;
        mode.Disabled = !enabled;
        modular.Disabled = !enabled;
        seed.Editable = enabled;
    }

    internal void ShowFailure(ClientStartupError failure)
    {
        ApplyProgress(GameProgressPresentation.Unavailable(failure));
    }
}
