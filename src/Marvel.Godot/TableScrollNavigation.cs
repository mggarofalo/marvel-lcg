using Godot;

namespace Marvel.Godot;

/// <summary>Makes remaining content discoverable within a bounded table region.</summary>
internal static class TableScrollNavigation
{
    internal static ScrollContainer Create(Control parent, string name, string purpose, Rect2? bounds = null)
    {
        var frame = new VBoxContainer
        {
            Name = $"{name}Frame", MouseFilter = Control.MouseFilterEnum.Pass,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        if (bounds is { } region)
        {
            frame.Position = region.Position;
            frame.Size = region.Size;
        }
        parent.AddChild(frame);
        var scroll = new ScrollContainer
        {
            Name = name, FollowFocus = true, ClipContents = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever,
        };
        frame.AddChild(scroll);
        var navigation = new HBoxContainer
        {
            Name = "OverflowNavigation", ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        frame.AddChild(navigation);
        Button earlier = Button(navigation, "Earlier", $"↑ Earlier {purpose}");
        Button more = Button(navigation, "More", $"More {purpose} ↓");
        VScrollBar range = scroll.GetVScrollBar();
        void Refresh()
        {
            if (!InteractionControl.IsUsable(scroll)) return;
            bool above = range.Value > range.MinValue + 1;
            bool below = range.Value + range.Page < range.MaxValue - 1;
            navigation.Visible = above || below;
            earlier.Visible = above;
            more.Visible = below;
        }
        void Move(int direction)
        {
            scroll.ScrollVertical += direction * Math.Max(1, (int)(scroll.Size.Y * 0.8f));
            Refresh();
        }
        earlier.Pressed += () => Move(-1);
        more.Pressed += () => Move(1);
        range.Changed += () => Callable.From(Refresh).CallDeferred();
        range.ValueChanged += _ => Refresh();
        scroll.Resized += () => Callable.From(Refresh).CallDeferred();
        navigation.Visible = false;
        Callable.From(Refresh).CallDeferred();
        return scroll;
    }

    private static Button Button(Container parent, string name, string text)
    {
        var button = new Button
        {
            Name = name, Text = name, AccessibilityName = text, TooltipText = text,
            CustomMinimumSize = new Vector2(0, 44),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.Off,
            ThemeTypeVariation = GodotThemeVariations.ChoiceButton,
        };
        parent.AddChild(button);
        TableCompactButtonStyle.Apply(button);
        return button;
    }
}
