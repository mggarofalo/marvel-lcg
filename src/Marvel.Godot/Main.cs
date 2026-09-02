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
    private OptionButton hero = null!;
    private LocalGameClient? localClient;
    private string? localCapability;
    private OptionButton mode = null!;
    private OptionButton modular = null!;
    private PanelContainer promptPanel = null!;
    private Label promptContext = null!;
    private Label promptEyebrow = null!;
    private Label promptHeading = null!;
    private Label promptProgress = null!;
    private Label promptRequirement = null!;
    private OptionButton scenario = null!;
    private LineEdit seed = null!;
    private Control setupPanel = null!;
    private SetupChoices? setupChoices;
    private Button start = null!;
    private Label status = null!;
    private PanelContainer statusPanel = null!;
    private Label title = null!;
    private bool decisionPending;

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
        hero.ItemSelected += _ => RefreshBriefing();
        scenario.ItemSelected += OnScenarioSelected;
        mode.ItemSelected += _ =>
        {
            PopulateModularChoices();
            RefreshBriefing();
        };
        modular.ItemSelected += _ => RefreshBriefing();
        seed.TextChanged += _ => RefreshStartAvailability();
        start.Pressed += OnStartPressed;
        _ = LoadSetupAsync();
    }

    private void ApplyInterfaceScale(InterfaceScale scale)
    {
        float minimumHeight = VisualSystem.Controls(scale).MinimumHeight;
        foreach (Control control in new Control[] { hero, scenario, mode, modular, seed })
        {
            control.CustomMinimumSize = new Vector2(
                control.CustomMinimumSize.X,
                minimumHeight);
        }

        start.CustomMinimumSize = new Vector2(
            start.CustomMinimumSize.X,
            Math.Max(start.CustomMinimumSize.Y, minimumHeight));
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
        hero = GetNode<OptionButton>($"{content}/Setup/Selections/Fields/Grid/Hero");
        scenario = GetNode<OptionButton>($"{content}/Setup/Selections/Fields/Grid/Scenario");
        mode = GetNode<OptionButton>($"{content}/Setup/Selections/Fields/Grid/Mode");
        modular = GetNode<OptionButton>($"{content}/Setup/Selections/Fields/Grid/Modular");
        seed = GetNode<LineEdit>($"{content}/Setup/Selections/Fields/Grid/Seed");
        start = GetNode<Button>($"{content}/Setup/Selections/Fields/Start");
        briefingScenario = GetNode<Label>(
            $"{content}/Setup/Briefing/Frame/Copy/Scenario");
        briefingMode = GetNode<Label>($"{content}/Setup/Briefing/Frame/Copy/Mode");
        briefingHero = GetNode<Label>($"{content}/Setup/Briefing/Frame/Copy/Hero");
        briefingModular = GetNode<Label>(
            $"{content}/Setup/Briefing/Frame/Copy/Modular");
        status = GetNode<Label>($"{content}/Status/Text");
        statusPanel = GetNode<PanelContainer>($"{content}/Status");
        CallDeferred(MethodName.ApplyResponsivePlayLayout);
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
        try
        {
            LocalClientConnection connection = LocalGameClient.ConnectLocal(
                ProjectSettings.GlobalizePath("res://../.."));
            if (!connection.Succeeded)
            {
                ShowFailure(connection.Error!);
                return;
            }

            localClient = connection.Client;
            ClientSetupResult setup = await localClient!.ReadSetupAsync();
            if (!setup.Succeeded)
            {
                ShowFailure(setup.Error!);
                return;
            }

            setupChoices = setup.Choices;
            PopulateSetupChoices();
            description.Text =
                "Choose an authored Core Set assignment. The engine validates it again when play starts.";
            status.Text = "ASSIGNMENT READY  ·  CHOOSE A HERO AND ENCOUNTER";
        }
        catch (Exception)
        {
            ShowFailure(new ClientStartupError(
                "startup_failed",
                "The setup screen could not be displayed. Restart the client to try again."));
        }
    }

    private void PopulateSetupChoices()
    {
        hero.Clear();
        foreach (HeroSetupChoice choice in setupChoices!.Heroes)
        {
            hero.AddItem(choice.Name);
        }

        scenarioNames.Clear();
        scenarioNames.AddRange(
            setupChoices.Scenarios.Select(choice => choice.Name)
                .Distinct(StringComparer.Ordinal));
        scenario.Clear();
        foreach (string name in scenarioNames)
        {
            scenario.AddItem(name);
        }

        hero.Disabled = false;
        scenario.Disabled = false;
        mode.Disabled = false;
        modular.Disabled = false;
        OnScenarioSelected(0);
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
        briefingHero.Text = setupChoices.Heroes[hero.Selected].Name;
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
        start.Disabled = setupChoices is null || !validSeed;
        if (setupChoices is not null && !validSeed)
        {
            status.Text = "SEED REQUIRED  ·  ENTER A WHOLE NUMBER FROM 0 THROUGH 4294967295";
        }
        else if (setupChoices is not null && CurrentGame is null)
        {
            status.Text = "ASSIGNMENT READY  ·  START WHEN THE TABLE IS SET";
        }
    }

    private async void OnStartPressed()
    {
        try
        {
            start.Disabled = true;
            SetSetupControlsEnabled(false);
            status.Text = "DEALING GAME  ·  ONE MOMENT";

            ClientStartupResult startup = await localClient!.OpenAsync(
                setupChoices!, SelectedSetup());
            if (!startup.Succeeded)
            {
                SetSetupControlsEnabled(true);
                RefreshStartAvailability();
                ShowFailure(startup.Error!);
                return;
            }

            localCapability = startup.Response!.Capability;
            RenderGame(startup.Response, resetEvents: true);
            setupPanel.Visible = false;
            board.Visible = true;
            eyebrow.Text = "CORE SET  /  LOCAL TABLE";
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

    private async void OnDecisionSubmitted(EngineDecision decision)
    {
        if (decisionPending)
        {
            return;
        }

        decisionPending = true;
        try
        {
            decisions.SetSubmitting(true);
            promptProgress.Text = "RESOLVING  ·  WAITING FOR ENGINE";
            promptProgress.ThemeTypeVariation = GodotThemeVariations.StatusText;
            ApplyProgress(GameProgressPresentation.Resolving());
            ClientResolutionResult result = await localClient!.ResolveAsync(
                localCapability!, decision);
            if (!IsInsideTree())
            {
                return;
            }

            if (result.HasAuthoritativeView)
            {
                RenderGame(result.Response!);
                decisionPending = false;
            }

            if (result.Error is not null)
            {
                if (result.HasAuthoritativeView)
                {
                    ApplyProgress(GameProgressPresentation.Recovered(
                        result.Response!, result.Error));
                }
                else
                {
                    ShowUnconfirmed(result.Error);
                }
            }
        }
        catch (Exception)
        {
            if (IsInsideTree())
            {
                ShowUnconfirmed(new ClientStartupError(
                    "display_failed",
                    "The decision result could not be displayed."));
            }
        }
    }

    private void ShowUnconfirmed(ClientStartupError error)
    {
        promptProgress.Text =
            $"UNCONFIRMED  ·  {error.Code.ToUpperInvariant()}  ·  RESTART OR RECONNECT";
        promptProgress.ThemeTypeVariation = GodotThemeVariations.DangerText;
        ApplyProgress(GameProgressPresentation.Unconfirmed(error));
    }

    private void RenderGame(EngineResponse response, bool resetEvents = false)
    {
        Outcome previousOutcome = CurrentGame?.World?.Outcome ?? Outcome.Unfinished;
        CurrentGame = response;
        WorldDescriptor world = response.World!;
        boardRender = BoardRenderer.Render(boardAreas, BoardPresentation.From(world));
        RenderPromptSummary(response.Prompt, world);
        decisions.Render(response.Prompt, world);
        EventBatchPresentation presented = EventCuePlanner.Plan(
            response.Events, world, previousOutcome);

        if (resetEvents)
        {
            events.Reset(presented.History);
        }
        else
        {
            events.Append(presented.History);
        }

        RenderEvents();
        ApplyProgress(GameProgressPresentation.FromResponse(response));
        PresentEvents(presented.Cues);
    }

    private void RenderPromptSummary(Prompt? prompt, WorldDescriptor world)
    {
        if (prompt is null)
        {
            promptEyebrow.Text = "GAME COMPLETE";
            promptHeading.Text = "No further decision is waiting.";
            promptContext.Text = "THE TABLE IS SETTLED";
            promptRequirement.Text = "RESOLVED";
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
        title.Text = progress.Title;
        description.Text = progress.Description;
        status.Text = progress.Status;
        bool danger = progress.Kind is GameProgressKind.VillainWins
            or GameProgressKind.PlayersLose
            or GameProgressKind.Unconfirmed
            or GameProgressKind.Unavailable;
        statusPanel.ThemeTypeVariation = danger
            ? GodotThemeVariations.DangerStatusPanel
            : GodotThemeVariations.StatusPanel;
        status.ThemeTypeVariation = danger
            ? GodotThemeVariations.DangerText
            : GodotThemeVariations.StatusText;
    }

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
        return new GameSetupSelection(
            setupChoices!.Heroes[hero.Selected].Key,
            SelectedCampaign().Key,
            configuration,
            modularKey,
            seed.Text);
    }

    private ScenarioSetupChoice SelectedCampaign() => visibleModes[mode.Selected];

    private void SetSetupControlsEnabled(bool enabled)
    {
        hero.Disabled = !enabled;
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
