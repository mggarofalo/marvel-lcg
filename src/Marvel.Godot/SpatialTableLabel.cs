using Godot;

namespace Marvel.Godot;

/// <summary>Adds upright captions that remain independent from rotated table objects.</summary>
internal static class SpatialTableLabel
{
    internal static void Add(
        Control surface,
        string name,
        string text,
        Vector2 position,
        float width = 0,
        int z = 18)
    {
        surface.AddChild(new Label
        {
            Name = name,
            Text = text,
            Position = position,
            ThemeTypeVariation = GodotThemeVariations.Eyebrow,
            ZIndex = z,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = width > 0 ? new Vector2(width, 22) : Vector2.Zero,
            CustomMinimumSize = width > 0 ? new Vector2(width, 22) : Vector2.Zero,
            ClipText = width > 0,
            TextOverrunBehavior = width > 0
                ? TextServer.OverrunBehavior.TrimEllipsis
                : TextServer.OverrunBehavior.NoTrimming,
        });
    }
}
