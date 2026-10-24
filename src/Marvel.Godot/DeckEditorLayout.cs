using Godot;

namespace Marvel.Godot;

/// <summary>Persistent deck context and three full-height work areas; secondary tasks open separately.</summary>
internal static class DeckEditorLayout
{
    internal static void Build(DeckEditorControls ui, Main main)
    {
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, 16);
        ui.Window.AddChild(margin);
        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 12);
        margin.AddChild(outer);
        Header(ui, outer);
        outer.AddChild(new HSeparator());
        Workbench(ui, outer);
        outer.AddChild(new HSeparator());
        Footer(ui, outer);
        ui.Window.AddChild(ui.Confirm);
        ui.Window.AddChild(ui.File);
        main.AddChild(ui.Window);
        ui.Window.GuiEmbedSubwindows = true;
    }

    private static void Header(DeckEditorControls ui, VBoxContainer outer)
    {
        var decks = new Button { Name = "SavedDecks", Text = "Decks ▾" };
        var deckMenu = new Button { Name = "DeckActions", Text = "Deck ▾" };
        Row(outer, decks, ui.Name, ui.Undo, ui.Redo, deckMenu);
        ui.Name.TooltipText = "Deck name · edits are preserved when you leave";
        ui.Hero.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        ui.Hero.CustomMinimumSize = new(220, 0);
        ui.Count.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        Row(outer, new Label { Text = "Identity" }, ui.Hero, ui.InspectIdentity, ui.Count, ui.Validity, ui.SaveState);
        var library = Popup(ui, decks, "Saved decks", new(440, 240));
        library.AddChild(ui.Library);
        Row(library, ui.Load, ui.Delete);
        ui.Load.Pressed += () => library.GetWindow().Hide();
        ui.Delete.Pressed += () => library.GetWindow().Hide();
        var menu = Popup(ui, deckMenu, "Deck actions", new(260, 1), true);
        Actions(menu, ui.Starter, ui.Empty, ui.Copy, ui.Save);
        menu.AddChild(new HSeparator());
        var import = new Button { Text = "Import…" };
        Actions(menu, import, ui.Export);
        menu.AddChild(new HSeparator());
        Actions(menu, ui.Clear);
        Import(ui, import);
        var validity = Popup(ui, ui.Validity, "Deck validation", new(580, 240));
        ui.Diagnostics.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        ui.Diagnostics.CustomMinimumSize = new(0, 120);
        validity.AddChild(ui.Diagnostics);
        Actions(validity, ui.RestoreRequired);
    }

    private static void Workbench(DeckEditorControls ui, VBoxContainer outer)
    {
        var work = new HBoxContainer { Name = "DeckWorkbench", SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        work.AddThemeConstantOverride("separation", 18);
        outer.AddChild(work);
        var pool = Column(work);
        var deck = Column(work);
        var preview = new VBoxContainer { CustomMinimumSize = new(420, 0) };
        work.AddChild(preview);
        Pool(ui, pool);
        Deck(ui, deck);
        preview.AddChild(new Label { Text = "Card preview" });
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        preview.AddChild(scroll);
        scroll.AddChild(ui.Preview);
        ui.Preview.AddChild(ui.Inspector);
    }

    private static void Pool(DeckEditorControls ui, VBoxContainer pool)
    {
        var select = new Button { Name = "PoolActions", Text = "…", TooltipText = "Pool selection" };
        Row(pool, Heading("Card pool"), ui.Add, select);
        Actions(Popup(ui, select, "Pool selection", new(230, 1), true), ui.SelectAll);
        var filters = new Button { Name = "DeckFilters", Text = "Filters…" };
        Row(pool, ui.Search, ui.ClearSearch, filters);
        ui.SearchError.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        Row(pool, ui.CardScope, ui.SearchError);
        pool.AddChild(ui.Cards);
        Row(pool, ui.PreviousPage, ui.Page, ui.NextPage);
        var filter = Popup(ui, filters, "Card filters", new(660, 350));
        Row(filter, new Label { Text = "Aspect" }, ui.Aspect, new Label { Text = "Type" }, ui.Kind);
        Row(filter, new Label { Text = "Order" }, ui.Sort);
        filter.AddChild(new HSeparator());
        filter.AddChild(new Label { Text = "Build a search expression" });
        Row(filter, ui.SearchField, ui.SearchOperator);
        Row(filter, ui.SearchValue, ui.AddFilter);
        filter.AddChild(new Label { Text = "Spaces: AND · |: OR · !: exclude · < >: compare", AutowrapMode = TextServer.AutowrapMode.WordSmart });
    }

    private static void Deck(DeckEditorControls ui, VBoxContainer deck)
    {
        var select = new Button { Name = "DeckSelection", Text = "…", TooltipText = "Deck selection" };
        Row(deck, Heading("Your deck"), ui.Remove, select);
        Actions(Popup(ui, select, "Deck selection", new(230, 1), true), ui.SelectDeck);
        ui.DropHint.CustomMinimumSize = new(0, 28);
        ui.DropHint.Visible = false;
        deck.AddChild(ui.DropHint);
        deck.AddChild(ui.Deck);
    }

    private static void Footer(DeckEditorControls ui, VBoxContainer outer)
    {
        ui.Status.CustomMinimumSize = new(0, 22);
        ui.Status.MaxLinesVisible = 2;
        outer.AddChild(ui.Status);
        ui.Seat.AddItem("Seat 1");
        ui.Seat.AddItem("Seat 2");
        ui.Seat.AddItem("Seat 3");
        ui.Seat.AddItem("Seat 4");
        var selected = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        ui.Selection.AutowrapMode = TextServer.AutowrapMode.Off;
        ui.Selection.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        ui.Selection.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        selected.AddChild(ui.Selection);
        selected.AddChild(ui.Deselect);
        ui.Seat.SizeFlagsVertical = ui.UseDeck.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        Row(outer, ui.Close, selected, ui.Seat, ui.UseDeck);
        ui.UseDeck.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = new Color(0.18f, 0.34f, 0.36f), ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 6, ContentMarginBottom = 6, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
        });
    }

    private static void Import(DeckEditorControls ui, Button trigger)
    {
        var content = Popup(ui, trigger, "Import a deck", new(660, 320));
        Row(content, ui.Import, ui.Published);
        ui.Import.Pressed += () => content.GetWindow().Hide();
        ui.Published.Pressed += () => content.GetWindow().Hide();
        content.AddChild(new Label { Text = "Or paste a public MarvelCDB deck URL" });
        content.AddChild(ui.Url);
        Row(content, ui.Fetch, ui.CancelFetch);
        content.AddChild(ui.ImportStatus);
        ui.ImportDialog = content.GetWindow();
    }

    private static VBoxContainer Popup(DeckEditorControls ui, Button trigger, string title, Vector2I size, bool anchored = false)
    {
        Window window = anchored ? new PopupPanel() : new Window();
        window.Title = title;
        window.Size = size;
        window.WrapControls = false;
        window.Transient = window.Exclusive = true;
        window.Visible = false;
        window.Theme = ui.Window.Theme;
        ui.Window.AddChild(window);
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, 14);
        window.AddChild(margin);
        var stack = new VBoxContainer();
        margin.AddChild(stack);
        var content = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        stack.AddChild(content);
        if (!anchored)
        {
            var done = new Button { Text = "Done", SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
                Shortcut = new Shortcut { Events = [new InputEventKey { Keycode = Key.Escape }] } };
            stack.AddChild(done);
            done.Pressed += window.Hide;
        }
        trigger.Pressed += () => OpenPopup(window, trigger, ui.Window, size, anchored);
        window.VisibilityChanged += () =>
        {
            if (!window.Visible && trigger.GetWindow().Visible) trigger.GrabFocus();
        };
        window.CloseRequested += window.Hide;
        window.WindowInput += input => { if (input is InputEventKey { Pressed: true, Keycode: Key.Escape }) window.Hide(); };
        return content;
    }

    private static void OpenPopup(Window window, Control trigger, Window owner, Vector2I size, bool anchored)
    {
        if (!anchored) { ShowCentered(window, owner, size); return; }
        size = size.Max((Vector2I)window.GetChild<Control>(0).GetCombinedMinimumSize());
        var position = (Vector2I)((owner.GuiEmbedSubwindows ? trigger.GlobalPosition : trigger.GetScreenPosition()) + new Vector2(0, trigger.Size.Y));
        position.X = Math.Min(position.X, (owner.GuiEmbedSubwindows ? 0 : owner.Position.X) + owner.Size.X - size.X - 16);
        window.Popup(new Rect2I(position, size));
        window.Size = size;
    }

    internal static void ShowCentered(Window window, Window owner, Vector2I requested)
    {
        var size = requested.Min(owner.Size - new Vector2I(32, 48)).Max(window.MinSize);
        var origin = owner.GuiEmbedSubwindows ? Vector2I.Zero : owner.Position;
        window.Popup(new Rect2I(origin + (owner.Size - size) / 2, size));
        window.Size = size;
    }

    private static void Actions(VBoxContainer parent, params Button[] buttons)
    {
        foreach (var button in buttons)
        {
            button.Alignment = HorizontalAlignment.Left;
            parent.AddChild(button);
            button.Pressed += () => button.GetWindow().Hide();
        }
    }

    private static Label Heading(string text) => new() { Text = text, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    private static VBoxContainer Column(HBoxContainer parent)
    {
        var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        parent.AddChild(column);
        return column;
    }
    private static void Row(VBoxContainer parent, params Control[] controls)
    {
        var row = new HBoxContainer();
        parent.AddChild(row);
        foreach (var control in controls) row.AddChild(control);
    }
}
