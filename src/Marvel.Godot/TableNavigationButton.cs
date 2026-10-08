using Godot;

namespace Marvel.Godot;

/// <summary>Styles explicit continuation controls consistently across bounded table surfaces.</summary>
internal static class TableNavigationButton
{
    internal static Button Create(Container parent, string name, string explanation)
    {
        var button = new Button
        {
            Name = name, Text = name, AccessibilityName = explanation, TooltipText = explanation,
            CustomMinimumSize = new Vector2(0, 44),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.Off,
            ThemeTypeVariation = GodotThemeVariations.ChoiceButton,
        };
        parent.AddChild(button);
        TableCompactButtonStyle.Apply(button);
        return button;
    }
}
