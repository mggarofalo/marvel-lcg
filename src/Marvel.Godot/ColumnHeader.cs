using Godot;

namespace Marvel.Godot;

/// <summary>Building block: a column's one-line title and its row of fact icons, explained on hover.</summary>
/// <remarks>
/// The title ellipsizes instead of wrapping, so the header is always one line tall; each icon
/// carries its own fact's tooltip and the title carries the column's full explanation.
/// </remarks>
internal static class ColumnHeader
{
    /// <summary>The header's height at a reading scale: one heading line or one icon, whichever is taller.</summary>
    internal static float Height(InterfaceScale scale) =>
        Math.Max(VisualSystem.Type(scale).Heading * 1.4f, TableBlockStyle.Icon(scale)) + VisualSystem.Spacing(scale).ExtraSmall;

    /// <summary>The header's facts in display order, as their keys; native checks read them from <c>table_facts</c>.</summary>
    internal static string[] Keys(IReadOnlyList<TableFact> facts) => [.. facts.Select(fact => fact.ToString())];

    /// <summary>A header with a title, an optional step tracker and one icon per fact.</summary>
    internal static HBoxContainer Create(string name, string title, IReadOnlyList<TableFact> facts, string tooltip,
        InterfaceScale scale, Control? trailing = null)
    {
        var header = new HBoxContainer
        {
            Name = name, CustomMinimumSize = new Vector2(0, Height(scale)), MouseFilter = Control.MouseFilterEnum.Pass,
            TooltipText = tooltip, AccessibilityName = title, AccessibilityDescription = tooltip,
        };
        header.AddThemeConstantOverride("separation", VisualSystem.Spacing(scale).ExtraSmall);
        header.SetMeta("table_facts", Keys(facts));
        Label label = TableBlockStyle.Line("Title", title, scale, VisualSystem.Palette.Text);
        label.TooltipText = tooltip;
        label.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Heading);
        header.AddChild(label);
        if (trailing is not null) header.AddChild(trailing);
        int icon = TableBlockStyle.Icon(scale);
        foreach (TableFact fact in facts)
        {
            Wording wording = TableWording.For(fact);
            TextureRect mark = TableIcon.Create(wording.Icon, icon, ClientTheme.ToGodot(Tint(fact)));
            mark.Name = "Fact" + fact;
            mark.TooltipText = wording.Tooltip;
            mark.AccessibilityName = wording.Label;
            mark.MouseFilter = Control.MouseFilterEnum.Pass;
            mark.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            header.AddChild(mark);
        }
        return header;
    }

    // Color repeats what the icon's shape already says; it never carries a fact alone.
    private static VisualColor Tint(TableFact fact) => fact switch
    {
        TableFact.Acting => VisualSystem.Palette.Danger,
        TableFact.YourArea or TableFact.Destination => VisualSystem.Palette.Legal,
        TableFact.OutOfReach or TableFact.Retained => VisualSystem.Palette.MutedText,
        _ => VisualSystem.Palette.Text,
    };
}
