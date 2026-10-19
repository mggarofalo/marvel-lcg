using Godot;

namespace Marvel.Godot;

/// <summary>A focusable card row with separate inspection, selection and quantity gestures.</summary>
internal sealed partial class DeckCardRow : PanelContainer
{
    private readonly DeckCardTable table;
    private readonly Label title = new() { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
    private readonly Label detail = new() { MouseFilter = MouseFilterEnum.Ignore, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
    private readonly Label locked = new() { MouseFilter = MouseFilterEnum.Ignore };
    private readonly Button[] quantities = new Button[4];
    private bool deferredClick;
    internal string Face { get; }
    internal int Index { get; set; }

    internal DeckCardRow(DeckCardTable table, string face)
    {
        this.table = table;
        Face = face;
        Name = "Card_" + face;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new(0, 42);
        var body = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(body);
        var words = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new(110, 0) };
        words.AddThemeConstantOverride("separation", 0);
        detail.AddThemeFontSizeOverride("font_size", Math.Max(11, VisualSystem.Type(table.UiScale).Body - 2));
        detail.AddThemeColorOverride("font_color", new Color(0.61f, 0.68f, 0.71f));
        locked.AddThemeFontSizeOverride("font_size", Math.Max(11, VisualSystem.Type(table.UiScale).Body - 2));
        body.AddChild(words);
        words.AddChild(title);
        words.AddChild(detail);
        int size = DeckEditorTheme.QuantitySize(table.UiScale);
        var slot = new MarginContainer { Name = "QuantityColumn", MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new(size * 4 + 6, 0), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        var controls = new HBoxContainer { Name = "QuantityChoices", MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        controls.AddThemeConstantOverride("separation", 2);
        body.AddChild(slot);
        slot.AddChild(controls);
        controls.AddChild(locked);
        for (int quantity = 0; quantity <= 3; quantity++)
        {
            int captured = quantity;
            var button = new Button { Name = "Quantity" + quantity, Text = quantity.ToString(System.Globalization.CultureInfo.InvariantCulture), ThemeTypeVariation = "DeckQuantity", ToggleMode = true, CustomMinimumSize = new(size, size), SizeFlagsVertical = SizeFlags.ShrinkCenter };
            quantities[quantity] = button;
            controls.AddChild(button);
            button.FocusEntered += () => table.Inspected?.Invoke(Face);
            button.Pressed += () => { button.SetPressedNoSignal(!button.ButtonPressed); table.QuantityRequested?.Invoke(Face, captured); };
        }
        FocusEntered += () => table.Inspected?.Invoke(Face);
    }

    internal void Update(DeckTableEntry entry)
    {
        title.Text = entry.Count == 0 ? entry.Title : $"{entry.Count} × {entry.Title}";
        detail.Text = entry.Detail;
        TooltipText = entry.Title;
        locked.Visible = entry.Required;
        locked.Text = $"Required · {entry.Count}";
        for (int quantity = 0; quantity <= 3; quantity++)
        {
            var button = quantities[quantity];
            button.Visible = !entry.Required && entry.Quantities.Contains(quantity);
            button.SetPressedNoSignal(entry.Count == quantity);
            button.TooltipText = $"{entry.Title}: set total copies to {quantity}";
        }
    }

    internal void Select(bool selected) => AddThemeStyleboxOverride("panel", new StyleBoxFlat
    {
        BgColor = selected ? new Color(0.18f, 0.29f, 0.35f) : new Color(0.06f, 0.10f, 0.13f),
        ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 4, ContentMarginBottom = 4,
    });

    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse) Pointer(mouse);
        if (input is InputEventKey { Pressed: true } key && table.Key(Index, key)) AcceptEvent();
    }

    private void Pointer(InputEventMouseButton mouse)
    {
        if (mouse.Pressed)
        {
            GrabFocus();
            deferredClick = !mouse.CtrlPressed && !mouse.MetaPressed && !mouse.ShiftPressed && table.Selection.Contains(Index);
            if (!deferredClick) table.SelectRow(Index, mouse.CtrlPressed || mouse.MetaPressed, mouse.ShiftPressed);
        }
        else if (deferredClick) { deferredClick = false; table.SelectRow(Index, false, false); }
        AcceptEvent();
    }

    public override Variant _GetDragData(Vector2 position)
    {
        deferredClick = false;
        return table.BeginTransfer?.Invoke(this) ?? default;
    }
    public override bool _CanDropData(Vector2 position, Variant data) => table.AcceptTransfer?.Invoke(data) ?? false;
    public override void _DropData(Vector2 position, Variant data) => table.CompleteTransfer?.Invoke(data);
    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd) table.EndTransfer?.Invoke();
    }
}
