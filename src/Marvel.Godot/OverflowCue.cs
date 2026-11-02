using Godot;

namespace Marvel.Godot;

/// <summary>Building block: says that more is there than fits, and how many.</summary>
internal static class OverflowCue
{
    /// <summary>The cue's words, such as "+3 ▸"; nothing when nothing is hidden.</summary>
    internal static string Text(int hidden) => hidden > 0 ? $"+{hidden} ▸" : "";

    /// <summary>How many rows, each by its top and bottom edge, do not lie wholly inside the viewport's edges.</summary>
    /// <remarks>Half a pixel of rounding is tolerated, so a row flush with an edge counts as shown.</remarks>
    internal static int Hidden(IEnumerable<(float Top, float Bottom)> rows, float viewTop, float viewBottom) =>
        rows.Count(row => row.Top < viewTop - 0.5f || row.Bottom > viewBottom + 0.5f);

    /// <summary>A cue label, hidden when nothing overflows; the tooltip says where the rest is.</summary>
    internal static Label Create(string name, int hidden, string tooltip, InterfaceScale scale)
    {
        Label cue = TableBlockStyle.Whole(name, Text(hidden), scale, VisualSystem.Palette.Accent);
        cue.TooltipText = tooltip;
        cue.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        cue.HorizontalAlignment = HorizontalAlignment.Right;
        Show(cue, hidden);
        return cue;
    }

    /// <summary>Updates a cue to a new hidden count.</summary>
    internal static void Show(Label cue, int hidden)
    {
        cue.Text = Text(hidden);
        cue.Visible = hidden > 0;
    }
}
