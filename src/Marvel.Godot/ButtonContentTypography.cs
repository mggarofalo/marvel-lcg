using Godot;

namespace Marvel.Godot;

/// <summary>Gives composed button captions the same control weight as native button text.</summary>
internal static class ButtonContentTypography
{
    internal static void Apply(Node content)
    {
        if (content is Label label) label.AddThemeFontOverride("font", CardTypography.Bold);
        foreach (Node child in content.GetChildren()) Apply(child);
    }
}
