using Godot;

namespace Marvel.Godot;

/// <summary>Source-local cascading choices with readable costs and a single prompt lifetime.</summary>
internal sealed class CardContextMenu : IDisposable
{
    private static CardContextMenu? active;
    private readonly Control source;
    private readonly Func<bool> current;
    private readonly Action<CardMenuChoice> choose;
    private readonly Action inspect;
    private readonly CardPreviewOwnership previews;
    private readonly Theme theme;
    private readonly CanvasLayer layer;
    private readonly Control overlay;
    private readonly List<PanelContainer> panels = [];
    private readonly List<Button[]> rows = [];
    private readonly List<Button?> parents = [];

    private CardContextMenu(Control source, Func<bool> current, Action<CardMenuChoice> choose, Action inspect,
        CardPreviewOwnership previews, Theme theme)
    {
        this.source = source;
        this.current = current;
        this.choose = choose;
        this.inspect = inspect;
        this.previews = previews;
        this.theme = theme;
        layer = new CanvasLayer { Name = "CardContextMenu", Layer = 100 };
        Node host = source;
        while (host.GetParent() is Control parent) host = parent;
        host.AddChild(layer);
        overlay = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        layer.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        overlay.GuiInput += input =>
        {
            if (input is InputEventMouseButton { Pressed: true })
            {
                overlay.AcceptEvent();
                Dismiss();
            }
        };
    }

    internal static void Show(Control source, IReadOnlyList<CardMenuEntry> entries,
        Func<bool> isCurrent, Action<CardMenuChoice> choose, Action inspect,
        CardPreviewOwnership previews, Theme theme)
    {
        Close();
        BoardActionChoiceSurface.Close();
        var menu = new CardContextMenu(source, isCurrent, choose, inspect, previews, theme);
        active = menu;
        menu.Open(0, [.. entries, new CardMenuEntry("Inspect card", "", null, [])], null);
    }

    private void Open(int depth, IReadOnlyList<CardMenuEntry> entries, Button? parent, bool focus = true)
    {
        if (!Valid()) return;
        Trim(depth);
        var panel = new PanelContainer { Name = $"MenuLevel{depth}", Theme = theme,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
            CustomMinimumSize = new Vector2(340, 0), Size = new Vector2(340, 1) };
        var stack = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        panel.AddChild(scroll);
        scroll.AddChild(stack);
        stack.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        overlay.AddChild(panel);
        panels.Add(panel);
        parents.Add(parent);
        Button[] buttons = [.. entries.Select((entry, index) => AddRow(stack, entry, depth, index))];
        rows.Add(buttons);
        for (int index = 0; index < buttons.Length; index++)
        {
            buttons[index].FocusNext = buttons[(index + 1) % buttons.Length].GetPath();
            buttons[index].FocusPrevious = buttons[(index + buttons.Length - 1) % buttons.Length].GetPath();
        }
        Callable.From(() =>
        {
            if (!Valid() || depth >= panels.Count || panels[depth] != panel) return;
            Place(panel, stack, parent);
            Callable.From(() => { if (Valid() && GodotObject.IsInstanceValid(panel)) Place(panel, stack, parent); }).CallDeferred();
            if (focus) buttons.FirstOrDefault(button => !button.Disabled)?.GrabFocus();
        }).CallDeferred();
    }

    private Button AddRow(VBoxContainer stack, CardMenuEntry entry, int depth, int index)
    {
        string text = entry.Label + (entry.Children.Count > 0 ? "  ›" : "");
        if (!string.IsNullOrWhiteSpace(entry.Detail)) text += "\n" + entry.Detail;
        var button = new Button { Name = $"Menu{depth}Item{index}", Text = text,
            Disabled = entry.Disabled, Alignment = HorizontalAlignment.Left,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(320, 36), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AccessibilityName = text, ThemeTypeVariation = GodotThemeVariations.TertiaryButton };
        if (entry.Preview is { } target) button.SetMeta("offered_target_anchor", target);
        stack.AddChild(button);
        button.Pressed += () => Activate(entry, depth, button);
        button.GuiInput += input => Navigate(input, entry, depth, index, button);
        button.FocusEntered += () => Preview(button, entry);
        button.FocusExited += () => previews.Dismiss(button);
        button.MouseExited += () => previews.Dismiss(button);
        button.MouseEntered += () =>
        {
            Preview(button, entry);
            if (!Valid()) return;
            if (entry.Children.Count > 0) Open(depth + 1, entry.Children, button, focus: false);
            else Trim(depth + 1);
        };
        return button;
    }

    private void Preview(Control owner, CardMenuEntry entry)
    {
        if (Valid() && entry.Preview is { } target) previews.Show(owner, target);
    }

    private void Activate(CardMenuEntry entry, int depth, Button button)
    {
        if (!Valid() || entry.Disabled) return;
        if (entry.Children.Count > 0) { Open(depth + 1, entry.Children, button); return; }
        Close();
        if (entry.Choice is { } choice) choose(choice);
        else inspect();
    }

    private void Navigate(InputEvent input, CardMenuEntry entry, int depth, int index, Button button)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.Down: MoveFocus(depth, index, 1); break;
            case Key.Up: MoveFocus(depth, index, -1); break;
            case Key.Right:
                if (entry.Children.Count > 0) Open(depth + 1, entry.Children, button);
                break;
            case Key.Left:
            case Key.Escape: Back(depth); break;
            default: return;
        }
        button.AcceptEvent();
    }

    private void MoveFocus(int depth, int index, int direction)
    {
        Button[] buttons = rows[depth];
        for (int offset = 1; offset <= buttons.Length; offset++)
        {
            Button next = buttons[(index + direction * offset + buttons.Length) % buttons.Length];
            if (next.Disabled) continue;
            Trim(depth + 1);
            next.GrabFocus();
            return;
        }
    }

    private void Back(int depth)
    {
        if (depth == 0) { Dismiss(); return; }
        Button? parent = parents[depth];
        Trim(depth);
        if (InteractionControl.IsUsable(parent)) parent!.GrabFocus();
    }

    private void Place(PanelContainer panel, VBoxContainer stack, Button? parent)
    {
        Vector2 viewport = source.GetViewportRect().Size;
        Rect2 anchor = parent?.GetGlobalRect() ?? source.GetGlobalRect();
        float width = Math.Min(380, viewport.X - 24);
        float height = Math.Min(stack.GetCombinedMinimumSize().Y + 40, viewport.Y - 24);
        float x = anchor.End.X + 4;
        if (x + width > viewport.X - 12) x = anchor.Position.X - width - 4;
        panel.Size = new Vector2(width, height);
        panel.Position = new Vector2(Math.Clamp(x, 12, viewport.X - width - 12),
            Math.Clamp(anchor.Position.Y, 12, viewport.Y - height - 12));
    }

    private void Trim(int depth)
    {
        while (panels.Count > depth)
        {
            PanelContainer panel = panels[^1];
            overlay.RemoveChild(panel);
            panel.QueueFree();
            panels.RemoveAt(panels.Count - 1);
            rows.RemoveAt(rows.Count - 1);
            parents.RemoveAt(parents.Count - 1);
        }
    }

    private bool Valid() => ReferenceEquals(active, this) && current()
        && InteractionControl.IsUsable(source);

    internal static bool RouteInput()
    {
        if (active is not { } menu) return false;
        if (!menu.Valid()) Close();
        return true;
    }

    private void Dismiss()
    {
        bool restore = Valid();
        Close();
        if (restore && InteractionControl.IsUsable(source)) source.GrabFocus();
    }

    internal static void Close()
    {
        CardContextMenu? menu = active;
        active = null;
        if (menu is null || !GodotObject.IsInstanceValid(menu.layer)) return;
        menu.previews.Clear();
        menu.Dispose();
    }

    public void Dispose()
    {
        if (GodotObject.IsInstanceValid(layer)) layer.QueueFree();
    }
}
