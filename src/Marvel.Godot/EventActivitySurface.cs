using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A passive native result stage in the table seam. History owns the complete chronology.</summary>
internal static class EventActivitySurface
{
    internal static void Add(Control table, Rect2 bounds)
    {
        var activity = new VBoxContainer { Name = "EventActivity", Position = bounds.Position,
            Size = bounds.Size, CustomMinimumSize = bounds.Size, Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 65 };
        activity.AddThemeConstantOverride("separation", 2);
        table.AddChild(activity);
        var tiles = new HBoxContainer { Name = "ActivityTiles", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
        tiles.AddThemeConstantOverride("separation", 6);
        activity.AddChild(tiles);
    }

    internal static void Present(Control host, EventActivitySequence sequence)
    {
        host.Visible = sequence.Entries.Count > 0;
        host.SetMeta("activity_running", sequence.Active);
        host.SetMeta("activity_index", sequence.Index);
        host.SetMeta("activity_count", sequence.Entries.Count);
        host.AccessibilityName = "Latest accepted results. Open History for complete resolution order.";
        HBoxContainer tiles = host.GetNode<HBoxContainer>("ActivityTiles");
        foreach (Node child in tiles.GetChildren()) { tiles.RemoveChild(child); child.QueueFree(); }
        IReadOnlyList<EventPresentation> entries = sequence.Active && sequence.Current is { } current
            ? [current] : ActivityVisualMeaning.Settled(sequence.Entries);
        host.SetMeta("activity_tile_count", entries.Count);
        for (int index = 0; index < entries.Count; index++) AddTile(tiles, entries[index], index);
    }

    private static void AddTile(HBoxContainer tiles, EventPresentation entry, int index)
    {
        var tile = new HBoxContainer { Name = $"ActivityTile{index}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore, TooltipText = entry.Summary,
            AccessibilityName = entry.Summary };
        tiles.AddChild(tile);
        var glyph = new Control { Name = "ActivityGlyph", CustomMinimumSize = new Vector2(36, 40),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        tile.AddChild(glyph);
        if (entry.Activity?.Subjects.Count > 0)
            tile.AddChild(new Control { Name = "OccurrenceReference", CustomMinimumSize = new Vector2(40, 40),
                MouseFilter = Control.MouseFilterEnum.Ignore });
        var facts = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        facts.AddThemeConstantOverride("separation", 0);
        tile.AddChild(facts);
        AddCopy(facts, "Kind", ActivityVisualMeaning.Title(entry), 10);
        AddCopy(facts, "Participants", ActivityVisualMeaning.Participants(entry), 12);
        AddCopy(facts, "Result", ActivityVisualMeaning.Result(entry), 18);
    }

    private static void AddCopy(VBoxContainer host, string name, string text, int size)
    {
        var label = new Label { Name = name, Text = text, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
        label.AddThemeFontSizeOverride("font_size", size);
        host.AddChild(label);
    }
}
