using Godot;

namespace Marvel.Godot;

/// <summary>Paints shallow stack silhouettes without expanding pile content or input bounds.</summary>
internal static class PileMaterialDepth
{
    internal static void Add(Control surface, PanelContainer panel)
    {
        var backing = new Control
        {
            Name = "PileStackDepth", Position = panel.Position, Size = panel.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = panel.ZIndex - 1,
        };
        surface.AddChild(backing);
        var rear = new StyleBoxFlat
        {
            BgColor = ClientTheme.ToGodot(ButtonMaterials.SecondaryPressed),
            BorderColor = new Color(ClientTheme.ToGodot(VisualSystem.Palette.Outline), 0.6f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
        };
        backing.Draw += () =>
        {
            backing.DrawStyleBox(rear, new Rect2(new Vector2(4, 4), backing.Size));
            backing.DrawStyleBox(rear, new Rect2(new Vector2(2, 2), backing.Size));
        };
        backing.TreeExiting += rear.Dispose;
    }
}
