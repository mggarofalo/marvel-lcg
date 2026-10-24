using Marvel.Protocol;
using System.Globalization;
using Godot;
using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Owns setup field choices, their presentation, and entry availability.</summary>
internal sealed class SetupSelectionControls
{
    private readonly Main main;
    private DeckSetupController? decks;
    private CampaignController? campaigns;
    private readonly ScenarioModeControls modes;
    private HeroSetupControls? heroes;

    internal GameSetupSelection ApplyDecks(GameSetupSelection setup) => decks?.Apply(setup) ?? setup;

    internal SetupSelectionControls(Main main)
    { this.main = main; modes = new(main); }

    internal ScenarioSetupChoice SelectedCampaign() => modes.Selected;
    internal IReadOnlyList<OptionButton> VariantControls => modes.Variants;
    internal IReadOnlyList<OptionButton> AdditionalHeroes => heroes?.Additional ?? [];
    internal IReadOnlyList<string> SelectedHeroes() => heroes!.Selected();
    internal void AssignHero(string key, int seat) => heroes!.Assign(key, seat);
    internal void RefreshHeroes() => heroes?.Refresh();

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
        decks ??= new DeckSetupController(main);
        campaigns ??= new CampaignController(main);
    }

    internal void PopulateSecondHeroChoices()
    {
        heroes ??= new(main);
        heroes.Refresh();
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
        modes.Populate();
        PopulateModularChoices();
        RefreshBriefing();
    }

    internal void PopulateModularChoices()
    {
        if (main.setupChoices is null || main.visibleModes.Count == 0)
        {
            return;
        }

        modes.Refresh();
        ScenarioSetupChoice campaign = main.SelectedCampaign();
        if (!campaign.AllowsModularSets)
        {
            main.modular.GetPopup().Clear();
            main.modularConfiguration = ModularConfiguration.None;
            main.selectedModularKeys.Clear();
            main.modular.Text = "No modular sets";
            return;
        }
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
        main.briefingMode.Text = ScenarioModeControls.Label(campaign.Mode)
            + (campaign.Variants.Count == 0 ? "" : " · " + string.Join(" · ", campaign.Variants.Select(item => $"{item.Name} {item.Value}")));
        heroes!.Refresh();
        main.briefingHero.Text = string.Join(" + ", heroes.Selected().Select(key =>
            main.setupChoices.Heroes.Single(hero => hero.Key == key).Name));
        main.briefingModular.Text = main.modular.Text;
        RefreshStartAvailability();
        SetupEntryPresentation.Configure(main);
    }

    internal void RefreshStartAvailability()
    {
        bool validSeed = string.IsNullOrWhiteSpace(main.seed.Text) || uint.TryParse(
            main.seed.Text.Trim(),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out _);
        main.start.Disabled = main.lifecycle.EntryPending || main.setupChoices is null
            || !validSeed || main.gameId.Text.Length == 0;
        if (main.setupChoices is not null && !validSeed)
        {
            main.status.Text = "Enter a seed from 0 to 4294967295, or leave it blank.";
            main.statusPanel.Visible = true;
        }
        else if (main.setupChoices is not null && main.CurrentGame is null)
        {
            main.status.Text = string.Empty;
            main.statusPanel.Visible = false;
        }
        SetupEntryPresentation.RefreshAvailability(main);
    }

    internal void RefreshEntryAvailability()
    {
        RefreshStartAvailability();
        main.join.Disabled = main.lifecycle.EntryPending || main.endpoint.Text.Length == 0
            || main.gameId.Text.Length == 0
            || main.invitation.Text.Length == 0;
        if (!main.joining) return;
        main.statusPanel.Visible = main.join.Disabled;
        if (main.endpoint.Text.Length == 0)
        {
            main.status.Text = "Enter the endpoint of the running engine.";
        }
        else if (main.gameId.Text.Length == 0)
        {
            main.status.Text = "Enter the game label shared by the host.";
        }
        else if (main.invitation.Text.Length == 0)
        {
            main.status.Text = "Paste the seat invitation shared by the host.";
        }
        else
        {
            main.status.Text = string.Empty;
        }
    }

}
