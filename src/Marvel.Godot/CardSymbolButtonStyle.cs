using Godot;

namespace Marvel.Godot;

/// <summary>Gives card-local symbols a generous hit area without an opaque caption panel.</summary>
internal static class CardSymbolButtonStyle
{
    internal const float HitSize = 44;

    internal static void Apply(Button button, bool onPaper = false)
    {
        button.CustomMinimumSize = new Vector2(HitSize, HitSize);
        button.Size = button.CustomMinimumSize;
        button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        button.AutowrapMode = TextServer.AutowrapMode.Off;
        button.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        button.AddThemeFontSizeOverride("font_size", 22);
        if (onPaper)
        {
            button.AddThemeColorOverride("font_color", CardFaceStyle.Ink);
            button.AddThemeColorOverride("font_disabled_color", CardFaceStyle.Ink with { A = 0.5f });
            foreach (string state in new[] { "hover", "pressed", "hover_pressed", "focus" })
                button.AddThemeColorOverride($"font_{state}_color", CardFaceStyle.Paper);
        }
        using var empty = new StyleBoxEmpty();
        button.AddThemeStyleboxOverride("normal", empty);
        button.AddThemeStyleboxOverride("disabled", empty);
        foreach (string state in new[] { "hover", "pressed", "hover_pressed", "focus" })
        {
            using var style = new StyleBoxFlat
            {
                BgColor = new Color(0.06f, 0.12f, 0.16f, 0.9f),
                BorderColor = new Color(1, 1, 1, state == "focus" ? 1 : 0.4f),
                BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                CornerRadiusTopLeft = 22, CornerRadiusTopRight = 22,
                CornerRadiusBottomLeft = 22, CornerRadiusBottomRight = 22,
            };
            button.AddThemeStyleboxOverride(state, style);
        }
    }
}
