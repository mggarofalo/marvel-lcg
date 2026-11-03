using Godot;

namespace Marvel.Godot;

/// <summary>
/// The draft on the table as one wrapping row: what is staged, selected or paid as trackers with pips, and every
/// other part as a chip of its words, whole on hover.
/// </summary>
internal static class DraftFactsRow
{
    // Our choice: a part longer than this many characters is bounded to the width below at 100%.
    private const int LongPart = 56;
    private const float LongPartWidth = 440;

    /// <summary>An empty row, hidden until a draft fills it.</summary>
    internal static HFlowContainer Create(InterfaceScale scale)
    {
        var row = new HFlowContainer
        {
            Name = "DraftFacts", Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        int gap = VisualSystem.Spacing(scale).Small;
        row.AddThemeConstantOverride("h_separation", gap);
        row.AddThemeConstantOverride("v_separation", VisualSystem.Spacing(scale).ExtraSmall);
        row.SetMeta("reading_scale", (int)scale);
        return row;
    }

    /// <summary>Replaces the row's contents with <paramref name="parts"/>; an empty list hides it.</summary>
    internal static void Fill(HFlowContainer row, IReadOnlyList<DraftPart> parts)
    {
        foreach (Node child in row.GetChildren())
        {
            row.RemoveChild(child);
            child.QueueFree();
        }
        var scale = (InterfaceScale)row.GetMeta("reading_scale", (int)InterfaceScale.Percent100).AsInt32();
        for (int index = 0; index < parts.Count; index++) row.AddChild(Part(parts[index], index, scale));
        row.Visible = parts.Count > 0;
    }

    /// <summary>Caps each long part's bounded line at <paramref name="width"/>, for a row inside a narrower surface.</summary>
    internal static void Bound(HFlowContainer row, float width)
    {
        foreach (Label label in row.FindChildren("Text", "Label", true, false).OfType<Label>())
            if (label.ClipText && label.CustomMinimumSize.X > width)
                label.CustomMinimumSize = new Vector2(Math.Max(0, width), 0);
    }

    /// <summary>Whether a part shows as a tracker rather than a chip.</summary>
    internal static bool Tracks(DraftPart part) => part.Label is not null && part.Total > 0;

    private static Control Part(DraftPart part, int index, InterfaceScale scale)
    {
        if (Tracks(part))
            return Tracker.Create($"DraftTracker{index}",
                new TrackerState(part.Label!, part.Filled, part.Total, null, 0, part.Text), scale);
        PanelContainer chip = Chip.Create($"DraftFact{index}", new Wording("", part.Label ?? part.Text, part.Text), null, scale);
        if (part.Text.Length > LongPart && chip.FindChild("Text", true, false) is Label label)
        {
            // A long part keeps one bounded line on the table and stays whole on hover.
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            label.ClipText = true;
            label.CustomMinimumSize = new Vector2(LongPartWidth * (int)scale / 100f, 0);
        }
        return chip;
    }
}
