using Godot;

namespace Marvel.Godot;

/// <summary>Building block: says that more is there than fits, and how many.</summary>
internal static class OverflowCue
{
    /// <summary>The cue's words, such as "+3 ▸"; nothing when nothing is hidden.</summary>
    internal static string Text(int hidden) => hidden > 0 ? $"+{hidden} ▸" : "";

    /// <summary>How many of <paramref name="count"/> equal rows of <paramref name="row"/> pixels a viewport cannot show.</summary>
    internal static int Hidden(int count, float row, float viewport) =>
        row <= 0 ? 0 : Math.Max(0, count - (int)Math.Floor(viewport / row));

    /// <summary>A cue label, hidden when nothing overflows; the tooltip says where the rest is.</summary>
    internal static Label Create(string name, int hidden, string tooltip, InterfaceScale scale)
    {
        Label cue = TableBlockStyle.Line(name, Text(hidden), scale, VisualSystem.Palette.Accent);
        cue.TooltipText = tooltip;
        cue.ClipText = false;
        cue.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        cue.HorizontalAlignment = HorizontalAlignment.Right;
        cue.Visible = hidden > 0;
        return cue;
    }
}
