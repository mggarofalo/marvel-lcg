using Godot;

namespace Marvel.Godot;

/// <summary>Building block: one short, scannable fact, an icon and a label of a few words, explained on hover.</summary>
internal static class Chip
{
    /// <summary>The chip's visible words: the fact's short label, then its count when it has one.</summary>
    internal static string Text(Wording wording, int? count) =>
        count is { } value ? $"{wording.Label} {value}" : wording.Label;

    /// <summary>A chip for a fact, with its tooltip as the accessible description.</summary>
    internal static PanelContainer Create(string name, Wording wording, int? count, InterfaceScale scale)
    {
        var chip = new PanelContainer
        {
            Name = name, TooltipText = wording.Tooltip, AccessibilityName = Text(wording, count),
            AccessibilityDescription = wording.Tooltip, MouseFilter = Control.MouseFilterEnum.Pass,
        };
        chip.SetMeta("table_fact_label", wording.Label);
        using StyleBoxFlat style = TableBlockStyle.Surface(scale, VisualSystem.Palette.Outline);
        chip.AddThemeStyleboxOverride("panel", style);
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", VisualSystem.Spacing(scale).ExtraSmall);
        if (wording.Icon.Length > 0)
            row.AddChild(TableIcon.Create(wording.Icon, TableBlockStyle.Icon(scale), ClientTheme.ToGodot(VisualSystem.Palette.Text)));
        Label label = TableBlockStyle.Line("Text", Text(wording, count), scale, VisualSystem.Palette.Text);
        label.ClipText = false;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        row.AddChild(label);
        chip.AddChild(row);
        return chip;
    }
}
