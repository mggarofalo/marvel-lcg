using Godot;

namespace Marvel.Godot;

/// <summary>A bounded response account beside, rather than replacing, the current choice.</summary>
internal static class EventActivitySurface
{
    internal static void Add(Control table, Rect2 bounds)
    {
        var activity = new VBoxContainer { Name = "EventActivity", Position = bounds.Position,
            Size = bounds.Size, CustomMinimumSize = bounds.Size, Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        activity.AddThemeConstantOverride("separation", 1);
        table.AddChild(activity);
        var heading = new Label { Name = "ActivityHeading", ThemeTypeVariation = GodotThemeVariations.Caption,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        heading.AddThemeFontSizeOverride("font_size", 11);
        activity.AddChild(heading);
        var scroll = new ScrollContainer { Name = "ActivityAccount", SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Reserve, FollowFocus = true,
            FocusMode = Control.FocusModeEnum.All };
        activity.AddChild(scroll);
        var text = new Label { Name = "ActivityText", AutowrapMode = TextServer.AutowrapMode.Word,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        text.AddThemeFontSizeOverride("font_size", 13);
        scroll.AddChild(text);
        scroll.CustomMinimumSize = new Vector2(0, text.GetThemeFont("font").GetHeight(13));
    }

    internal static void Present(Control host, EventActivitySequence sequence)
    {
        host.Visible = sequence.Entries.Count > 0;
        host.SetMeta("activity_running", sequence.Active);
        host.SetMeta("activity_index", sequence.Index);
        host.SetMeta("activity_count", sequence.Entries.Count);
        if (sequence.Current is null) return;
        host.GetNode<Label>("ActivityHeading").Text = sequence.Active
            ? $"JUST RESOLVED · {sequence.Index + 1} of {sequence.Entries.Count}"
            : $"RESOLVED · {sequence.Entries.Count} updates · Scroll to review";
        Label text = host.GetNode<Label>("ActivityAccount/ActivityText");
        text.Text = sequence.Active
            ? $"{sequence.Index + 1}. {sequence.Current.CueSummary ?? sequence.Current.Summary}"
            : string.Join("\n", sequence.Entries.Select((entry, index) => $"{index + 1}. {entry.Summary}"));
        text.TooltipText = text.Text;
        host.GetNode<ScrollContainer>("ActivityAccount").ScrollVertical = 0;
    }
}
