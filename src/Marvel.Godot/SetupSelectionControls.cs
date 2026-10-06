using System.Globalization;
using Godot;
using Marvel.Client;
using Marvel.Server;

namespace Marvel.Godot;

/// <summary>Owns setup field choices, their presentation, and entry availability.</summary>
internal sealed class SetupSelectionControls
{
    private readonly Main main;

    internal SetupSelectionControls(Main main) => this.main = main;

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
        main.briefingMode.Text = campaign.Expert ? "Expert" : "Standard";
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
