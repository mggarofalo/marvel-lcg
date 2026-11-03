using Godot;
namespace Marvel.Godot;

/// <summary>The campaign screens' layout pieces, sized for the reading scale.</summary>
internal static class CampaignBlocks
{
    /// <summary>A wrapping row of chips, trackers and links with the design system's small gaps.</summary>
    internal static HFlowContainer Row(string name, InterfaceScale scale)
    {
        var row = new HFlowContainer { Name = name, MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        int gap = VisualSystem.Spacing(scale).Small;
        row.AddThemeConstantOverride("h_separation", gap);
        row.AddThemeConstantOverride("v_separation", VisualSystem.Spacing(scale).ExtraSmall);
        return row;
    }

    /// <summary>A heading line: a campaign's or a player's name.</summary>
    internal static Label Heading(string name, string text, InterfaceScale scale, string tooltip = "")
    {
        Label label = TableBlockStyle.Whole(name, text, scale, VisualSystem.Palette.Text);
        label.TooltipText = tooltip;
        label.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Heading);
        label.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        return label;
    }

    /// <summary>A screen's title line, a step larger than a heading.</summary>
    internal static Label Title(string name, string text, InterfaceScale scale, string tooltip = "")
    {
        Label label = Heading(name, text, scale, tooltip);
        label.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Heading * 5 / 4);
        return label;
    }

    /// <summary>A chip for each fact, in order.</summary>
    internal static void Chips(Container row, string prefix, IEnumerable<Wording> facts, InterfaceScale scale)
    {
        int index = 0;
        foreach (Wording fact in facts) row.AddChild(Chip.Create(prefix + index++, fact, null, scale));
    }

    /// <summary>A commitment button sized for the reading scale.</summary>
    internal static Button Action(Container parent, string text, Action action, InterfaceScale scale)
    {
        var button = new Button { Text = text, Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new Vector2(0, VisualSystem.Controls(scale).MinimumHeight) };
        button.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Body);
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }
}
