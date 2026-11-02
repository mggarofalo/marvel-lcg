using Godot;

namespace Marvel.Godot;

/// <summary>Draws one table state icon from <c>assets/icons/table</c>, tinted, at an exact size.</summary>
internal static class TableIcon
{
    /// <summary>A tinted icon that ignores the pointer, so its parent's tooltip and input apply.</summary>
    internal static TextureRect Create(string icon, int pixels, Color tint) => new()
    {
        Name = "Icon_" + icon, Texture = CardGlyphRendering.Rasterize($"Marvel.Godot.Assets.TableIcons.{icon}.svg", pixels),
        CustomMinimumSize = new Vector2(pixels, pixels), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, TextureFilter = CanvasItem.TextureFilterEnum.Linear,
        MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = tint,
    };
}
