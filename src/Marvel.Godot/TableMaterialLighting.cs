using Godot;

namespace Marvel.Godot;

/// <summary>Adds one static overhead wash behind the table's existing physical geometry.</summary>
internal static class TableMaterialLighting
{
    internal static void Add(Control surface)
    {
        var backing = new ColorRect
        {
            Name = "TableMaterialBacking", Color = ClientTheme.ToGodot(VisualSystem.Palette.Surface),
            MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = -1,
        };
        surface.AddChild(backing);
        backing.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var gradient = new Gradient
        {
            Offsets = [0, 0.55f, 1],
            Colors = [new Color(1, 1, 1, 0.045f), new Color(1, 1, 1, 0.01f), Colors.Transparent],
        };
        var texture = new GradientTexture2D
        {
            Gradient = gradient, Width = 512, Height = 256,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.52f, 0.3f), FillTo = new Vector2(1.17f, 0.3f),
        };
        var light = new TextureRect
        {
            Name = "StaticOverheadLight", Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        backing.AddChild(light);
        light.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        backing.TreeExiting += () => { light.Texture = null; texture.Dispose(); gradient.Dispose(); };
    }
}
