using Godot;

namespace Marvel.Godot;

/// <summary>A bounded setup sheet with exposed choices and optional connection details.</summary>
internal static class SetupEntryPresentation
{
    private const string FieldsPath = "Margin/Shell/Content/Setup/Selections/Fields";

    internal static void Configure(Main main)
    {
        if (!main.setupPanel.Visible) return;
        PanelContainer shell = main.GetNode<PanelContainer>("Margin/Shell");
        float width = Math.Min(1160, Math.Max(0, main.GetViewportRect().Size.X - 64));
        shell.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        shell.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        shell.CustomMinimumSize = new Vector2(width, 0);
        float side = Math.Max(16, (main.GetViewportRect().Size.X - width) / 2);
        main.pageScroll.OffsetLeft = side;
        main.pageScroll.OffsetRight = -side;
        ConfigureFooter(main, side);
        main.setupPanel.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        main.setupGrid.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        main.joinFields.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        main.setupGrid.Columns = 2;
        main.title.AddThemeFontSizeOverride("font_size", 28);
        main.description.AddThemeFontSizeOverride("font_size", 16);
        main.startFlow.Text = main.joining ? "Create table" : "✓ Create table";
        main.startFlow.ThemeTypeVariation = string.Empty;
        main.joinFlow.ThemeTypeVariation = string.Empty;
        main.joinFlow.Text = main.joining ? "✓ Join a game" : "Join a game";
        main.setupHeading.Text = "Choose your game";
        main.description.Text = main.joining
            ? "Use the connection details and seat invitation shared by the host."
            : "Choose your heroes and encounter. Your starting decks are included.";
        main.description.Visible = true;
        VBoxContainer fields = main.GetNode<VBoxContainer>(FieldsPath);
        Button advanced = EnsureAdvanced(main, fields);
        bool expanded = main.joining || advanced.ButtonPressed;
        fields.GetNode<Control>("ConnectionGrid").Visible = expanded;
        main.seed.Visible = expanded && !main.joining;
        main.setupGrid.GetNode<Control>("SeedLabel").Visible = main.seed.Visible;
        main.seedHelp.Visible = main.seed.Visible;
        main.seedHelp.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        main.reloadSetup.Visible = expanded && !main.joining;
        main.reloadSetup.Text = "Refresh available games";
        advanced.Visible = !main.joining;
        fields.MoveChild(main.setupHeading, 1);
        fields.MoveChild(main.setupGrid, 2);
        fields.MoveChild(advanced, 3);
        fields.MoveChild(fields.GetNode<Control>("ConnectionGrid"), 4);
        fields.MoveChild(main.reloadSetup, 5);
        fields.MoveChild(main.seedHelp, 6);
        RefreshChoices(main);
        RefreshAdditionalSeats(main);
        foreach (Control control in fields.FindChildren("*", "Control", true, false).OfType<Control>())
        {
            if (control is Label or BaseButton or LineEdit)
                control.AddThemeFontSizeOverride("font_size", Math.Max(16, Mathf.RoundToInt(12 + 8 * (int)main.interfaceScale / 100f)));
            if (control is BaseButton)
                control.CustomMinimumSize = new Vector2(control.CustomMinimumSize.X,
                    Math.Max(44, VisualSystem.Controls(main.interfaceScale).MinimumHeight));
        }
    }

    private static void ConfigureFooter(Main main, float side)
    {
        PanelContainer footer = EnsureFooter(main);
        footer.Visible = !main.joining;
        main.start.AddThemeFontSizeOverride("font_size", Math.Max(16, Mathf.RoundToInt(12 + 8 * (int)main.interfaceScale / 100f)));
        float height = Math.Max(64, VisualSystem.Controls(main.interfaceScale).MinimumHeight + 24);
        footer.OffsetLeft = side;
        footer.OffsetRight = -side;
        footer.OffsetTop = -height - 16;
        footer.OffsetBottom = -16;
        main.pageScroll.OffsetBottom = main.joining ? -16 : -height - 24;
    }

    private static PanelContainer EnsureFooter(Main main)
    {
        if (main.GetNodeOrNull<PanelContainer>("SetupFooter") is { } existing) return existing;
        var footer = new PanelContainer
        {
            Name = "SetupFooter", AnchorLeft = 0, AnchorRight = 1, AnchorTop = 1, AnchorBottom = 1,
            ThemeTypeVariation = "SurfacePanel",
        };
        main.AddChild(footer);
        main.start.Reparent(footer);
        footer.Visible = main.setupPanel.Visible;
        main.setupPanel.VisibilityChanged += () => footer.Visible = main.setupPanel.Visible && !main.joining;
        return footer;
    }

    private static void RefreshAdditionalSeats(Main main)
    {
        // Reveal another seat when the preceding seat is occupied, keeping the
        // default solo setup and its commitment within the sheet.
        var additional = main.setupController.AdditionalHeroes;
        int playerCount = main.setupChoices is null ? 1 : main.setupController.SelectedHeroes().Count;
        for (int index = 0; index < additional.Count; index++)
        {
            bool visible = playerCount >= index + 2;
            main.setupGrid.GetNode<Control>(additional[index].Name + "Choices").Visible = visible;
            main.setupGrid.GetNode<Control>("HeroSeatLabel" + (index + 2)).Visible = visible;
        }
    }

    internal static void RefreshChoices(Main main)
    {
        if (!main.setupPanel.Visible || main.setupChoices is null) return;
        foreach (OptionButton option in new[] { main.hero, main.secondHero, main.scenario, main.mode }
                     .Concat(main.setupController.VariantControls).Concat(main.setupController.AdditionalHeroes))
        {
            HFlowContainer row = EnsureRow(main.setupGrid, option);
            string? focus = FocusedChoice(row);
            Clear(row);
            for (int index = 0; index < option.ItemCount; index++)
            {
                int selected = index;
                Button button = Choice(option.GetItemText(index), index == option.Selected, option.Disabled);
                button.Pressed += () =>
                {
                    if (option.Disabled) return;
                    option.Select(selected);
                    option.EmitSignal(OptionButton.SignalName.ItemSelected, selected);

                };
                row.AddChild(button);
            }
            RestoreFocus(row, focus);
        }
        HFlowContainer modular = EnsureRow(main.setupGrid, main.modular);
        modular.Visible = main.SelectedCampaign().AllowsModularSets;
        main.setupGrid.GetNode<Control>("ModularLabel").Visible = modular.Visible;
        string? modularFocus = FocusedChoice(modular);
        Clear(modular);
        PopupMenu popup = main.modular.GetPopup();
        for (int index = 0; index < popup.ItemCount; index++)
        {
            if (popup.IsItemSeparator(index)) continue;
            long id = popup.GetItemId(index);
            Button button = Choice(popup.GetItemText(index), popup.IsItemChecked(index), main.modular.Disabled);
            button.Pressed += () => { if (!main.modular.Disabled) main.OnModularChoicePressed(id); };
            modular.AddChild(button);
        }
        RestoreFocus(modular, modularFocus);
    }

    private static string? FocusedChoice(Control row) =>
        row.GetViewport().GuiGetFocusOwner() is Button button && row.IsAncestorOf(button)
            ? button.AccessibilityName : null;

    private static void RestoreFocus(Control row, string? name)
    {
        if (name is null) return;
        Button? button = row.GetChildren().OfType<Button>()
            .FirstOrDefault(candidate => candidate.AccessibilityName == name);
        if (button is null) return;
        Callable.From(() =>
        {
            if (InteractionControl.IsUsable(button) && button.IsVisibleInTree()) button.GrabFocus();
        }).CallDeferred();
    }

    internal static void RefreshAvailability(Main main)
    {
        foreach (BaseButton adapter in new BaseButton[]
                 { main.hero, main.secondHero, main.scenario, main.mode, main.modular }
                     .Concat(main.setupController.VariantControls).Concat(main.setupController.AdditionalHeroes))
        {
            if (main.setupGrid.GetNodeOrNull<HFlowContainer>(adapter.Name + "Choices") is not { } row)
                continue;
            foreach (Button button in row.GetChildren().OfType<Button>()) button.Disabled = adapter.Disabled;
        }
    }

    internal static void SetAssignmentEnabled(Main main, bool enabled)
    {
        foreach (BaseButton adapter in new BaseButton[]
                 { main.hero, main.secondHero, main.scenario, main.mode, main.modular }
                     .Concat(main.setupController.VariantControls).Concat(main.setupController.AdditionalHeroes))
            adapter.Disabled = !enabled;
        main.seed.Editable = enabled;
        main.setupController.RefreshHeroes();
        RefreshAvailability(main);
    }

    private static HFlowContainer EnsureRow(GridContainer grid, BaseButton adapter)
    {
        adapter.Visible = false;
        adapter.FocusMode = Control.FocusModeEnum.None;
        string name = adapter.Name + "Choices";
        if (grid.GetNodeOrNull<HFlowContainer>(name) is { } existing) return existing;
        var row = new HFlowContainer
        {
            Name = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        grid.AddChild(row);
        grid.MoveChild(row, adapter.GetIndex());
        return row;
    }

    private static Button EnsureAdvanced(Main main, VBoxContainer fields)
    {
        if (fields.GetNodeOrNull<Button>("AdvancedSetup") is { } existing) return existing;
        var button = new Button
        {
            Name = "AdvancedSetup", Text = "Connection and seed", ToggleMode = true,
            CustomMinimumSize = new Vector2(0, 44),
        };
        button.Toggled += _ => Configure(main);
        fields.AddChild(button);
        return button;
    }

    private static Button Choice(string text, bool selected, bool disabled) => new()
    {
        Text = (selected ? "✓ " : "") + text,
        AccessibilityName = text,
        Disabled = disabled, CustomMinimumSize = new Vector2(0, 44),
        ThemeTypeVariation = selected ? GodotThemeVariations.MultiSelectButton : string.Empty,
    };

    private static void Clear(Control row)
    {
        foreach (Node child in row.GetChildren())
        {
            row.RemoveChild(child);
            child.QueueFree();
        }
    }
}
