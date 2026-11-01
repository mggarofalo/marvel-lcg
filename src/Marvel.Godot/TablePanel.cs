using Godot;

namespace Marvel.Godot;

/// <summary>Building block: a decision's title, its facts as chips and its reason, above its actions.</summary>
/// <remarks>
/// A table panel's reason is one line: a longer engine instruction ellipsizes and stays whole in the
/// tooltip. A decision sheet's reason wraps, because the player must read the whole instruction
/// before committing. The caller adds the actions below, so the panel never repeats what its
/// actions already say.
/// </remarks>
internal static class TablePanel
{
    /// <summary>The reason as one line: line breaks become separators and surrounding space is trimmed.</summary>
    internal static string OneLine(string reason) =>
        string.Join(" · ", reason.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    /// <summary>The panel head: the title row, the fact chips, then the reason when there is one.</summary>
    /// <param name="wrap">Whether the reason wraps in full rather than ellipsizing to one line.</param>
    internal static VBoxContainer Create(string name, Control title, IReadOnlyList<Control> facts, string reason,
        InterfaceScale scale, Control? trailing = null, bool wrap = false)
    {
        SpacingMetrics spacing = VisualSystem.Spacing(scale);
        var head = new VBoxContainer { Name = name, MouseFilter = Control.MouseFilterEnum.Ignore };
        head.AddThemeConstantOverride("separation", spacing.ExtraSmall);
        var titleRow = new HBoxContainer { Name = "TitleRow" };
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        titleRow.AddChild(title);
        if (trailing is not null) titleRow.AddChild(trailing);
        head.AddChild(titleRow);
        if (facts.Count > 0)
        {
            var chips = new HFlowContainer { Name = "Facts", MouseFilter = Control.MouseFilterEnum.Ignore };
            chips.AddThemeConstantOverride("h_separation", spacing.ExtraSmall);
            foreach (Control fact in facts) chips.AddChild(fact);
            head.AddChild(chips);
        }
        string line = wrap ? reason.Trim() : OneLine(reason);
        if (line.Length == 0) return head;
        Label text = wrap ? TableBlockStyle.Wrapped("Reason", line, scale, VisualSystem.Palette.Text)
            : TableBlockStyle.Line("Reason", line, scale, VisualSystem.Palette.Text);
        text.TooltipText = reason;
        text.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Body);
        head.AddChild(text);
        return head;
    }
}
