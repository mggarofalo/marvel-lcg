using Godot;

namespace Marvel.Godot;

/// <summary>Styles captions and commitments inside fixed-width table controls.</summary>
internal static class TableCompactButtonStyle
{
    internal static void Apply(Button button)
    {
        // The table's fixed-width controls share its region drawers' 14px type.
        // Padding leaves room for whole words and complete wrapped commitments.
        button.AddThemeFontSizeOverride("font_size", 14);
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
        {
            var style = (StyleBox)button.GetThemeStylebox(state).Duplicate();
            style.ContentMarginLeft = 0;
            style.ContentMarginRight = 0;
            style.ContentMarginTop = 4;
            style.ContentMarginBottom = 4;
            button.AddThemeStyleboxOverride(state, style);
        }
    }
}
