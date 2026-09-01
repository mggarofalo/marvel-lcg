using System.Globalization;
using Godot;
using Marvel.Server;

namespace Marvel.Godot;

/// <summary>The desktop client's composition boundary and root scene.</summary>
public sealed partial class Main : Control
{
    private readonly List<string> scenarioNames = [];
    private readonly List<ScenarioSetupChoice> visibleModes = [];
    private Control board = null!;
    private GridContainer boardAreas = null!;
    private Label briefingHero = null!;
    private Label briefingMode = null!;
    private Label briefingModular = null!;
    private Label briefingScenario = null!;
    private Label description = null!;
    private Label eyebrow = null!;
    private OptionButton hero = null!;
    private LocalGameClient? localClient;
    private OptionButton mode = null!;
    private OptionButton modular = null!;
    private OptionButton scenario = null!;
    private LineEdit seed = null!;
    private Control setupPanel = null!;
    private SetupChoices? setupChoices;
    private Button start = null!;
    private Label status = null!;
    private Label title = null!;

    /// <summary>The initial visibility-safe game view, retained for board rendering.</summary>
    public EngineResponse? OpenedGame { get; private set; }

    /// <inheritdoc />
    public override void _Ready()
    {
        GetWindow().MinSize = new Vector2I(1040, 680);
        BindNodes();
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

    private void BindNodes()
    {
        const string content = "Margin/Shell/Content";
        description = GetNode<Label>($"{content}/Description");
        eyebrow = GetNode<Label>($"{content}/Eyebrow");
        title = GetNode<Label>($"{content}/Title");
        setupPanel = GetNode<Control>($"{content}/Setup");
        board = GetNode<Control>($"{content}/Board");
        boardAreas = GetNode<GridContainer>($"{content}/Board/Margin/Areas");
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
        else if (setupChoices is not null && OpenedGame is null)
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

            OpenedGame = startup.Response;
            BoardRenderer.Render(
                boardAreas,
                BoardPresentation.From(OpenedGame!.World!));
            setupPanel.Visible = false;
            board.Visible = true;
            eyebrow.Text = "CORE SET  /  LOCAL TABLE";
            title.Text = "The table is live.";
            description.Text =
                $"{briefingHero.Text} versus {briefingScenario.Text} · "
                + $"{OpenedGame!.World!.Areas.Count} visible areas · "
                + $"{OpenedGame.Events.Count} setup events";
            status.Text =
                $"GAME OPEN  ·  {OpenedGame.World.Outcome.ToString().ToUpperInvariant()}  ·  "
                + OpenedGame.Prompt!.Asking.ToString().ToUpperInvariant();
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
        description.Text = failure.Message;
        status.Text = $"GAME UNAVAILABLE  ·  {failure.Code.ToUpperInvariant()}";
    }
}
