using Godot;

namespace Marvel.Godot;

/// <summary>Gives card-local symbols a generous hit area without an opaque caption panel.</summary>
internal static class CardSymbolButtonStyle
{
    internal const float HitSize = 44;

    internal static void Apply(Button button)
    {
        button.CustomMinimumSize = new Vector2(HitSize, HitSize);
        button.Size = button.CustomMinimumSize;
        button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        button.AutowrapMode = TextServer.AutowrapMode.Off;
        button.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        button.AddThemeFontSizeOverride("font_size", 22);
        button.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        button.AddThemeStyleboxOverride("disabled", new StyleBoxEmpty());
        foreach (string state in new[] { "hover", "pressed", "hover_pressed", "focus" })
            button.AddThemeStyleboxOverride(state, new StyleBoxFlat
            {
                BgColor = new Color(0.06f, 0.12f, 0.16f, 0.9f),
                BorderColor = new Color(1, 1, 1, state == "focus" ? 1 : 0.4f),
                BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                CornerRadiusTopLeft = 22, CornerRadiusTopRight = 22,
                CornerRadiusBottomLeft = 22, CornerRadiusBottomRight = 22,
            });
    }
}
