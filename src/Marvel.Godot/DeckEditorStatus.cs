using Godot;

namespace Marvel.Godot;

/// <summary>The deck editor's status line and fact chips: a few words visible, the explanation on hover.</summary>
internal static class DeckEditorStatus
{
    /// <summary>The card search's expression syntax, shown on the search fields' hover.</summary>
    internal const string SearchSyntax = "Spaces: AND · |: OR · !: exclude · < >: compare";

    /// <summary>States what happened in a few words; the detail is the line's hover.</summary>
    internal static void Say(DeckEditorControls ui, string what, string detail)
    {
        ui.Status.Text = what;
        ui.Status.TooltipText = detail;
        ui.Status.MouseFilter = Control.MouseFilterEnum.Pass;
    }

    /// <summary>Frames a live label, such as the card count, as a design-system chip.</summary>
    internal static PanelContainer Chip(string name, Label label, InterfaceScale scale)
    {
        var chip = new PanelContainer { Name = name, MouseFilter = Control.MouseFilterEnum.Pass,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        using StyleBoxFlat style = TableBlockStyle.Surface(scale, VisualSystem.Palette.Outline);
        chip.AddThemeStyleboxOverride("panel", style);
        label.VerticalAlignment = VerticalAlignment.Center;
        chip.AddChild(label);
        return chip;
    }
}
