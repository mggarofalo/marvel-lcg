using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Reserves the table seam for the persistent timeline and records current attention progress.</summary>
internal static class EventActivitySurface
{
    internal static void Add(Control table, Rect2 bounds)
    {
        var activity = new VBoxContainer { Name = "EventActivity", Position = bounds.Position,
            Size = bounds.Size, CustomMinimumSize = bounds.Size, Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 65 };
        activity.AddThemeConstantOverride("separation", 2);
        table.AddChild(activity);
    }

    internal static void Present(Control host, EventActivitySequence sequence)
    {
        host.Visible = false;
        host.SetMeta("activity_running", sequence.Active);
        host.SetMeta("activity_index", sequence.Index);
        host.SetMeta("activity_count", sequence.BeatCount);
        host.SetMeta("receipt_count", sequence.Entries.Count);
    }
}
