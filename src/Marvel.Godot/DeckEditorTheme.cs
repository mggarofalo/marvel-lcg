using Godot;

namespace Marvel.Godot;

/// <summary>Compact desktop editing controls, scoped independently of gameplay actions.</summary>
internal static class DeckEditorTheme
{
    internal static Theme Create(InterfaceScale scale)
    {
        var theme = ClientTheme.Create(scale);
        theme.SetStylebox("panel", "PopupPanel", Surface(0.12f));
        foreach (string type in new[] { "Button", "MenuButton", "OptionButton", "LineEdit" })
        {
            theme.SetStylebox("normal", type, Surface(type is "LineEdit" or "OptionButton" ? 0.10f : 0.0f));
            theme.SetStylebox("hover", type, Surface(0.16f));
            theme.SetStylebox("pressed", type, Surface(0.22f));
            theme.SetStylebox("disabled", type, Surface(0.0f));
        }
        theme.SetConstant("separation", "HBoxContainer", 8);
        theme.SetConstant("separation", "VBoxContainer", 6);
        theme.SetTypeVariation("DeckQuantity", "Button");
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
        {
            var box = Surface(state is "pressed" or "hover_pressed" ? 0.28f : 0.10f);
            box.ContentMarginLeft = box.ContentMarginRight = 0;
            box.ContentMarginTop = box.ContentMarginBottom = 0;
            theme.SetStylebox(state, "DeckQuantity", box);
        }
        return theme;
    }

    internal static int QuantitySize(InterfaceScale scale) => Math.Max(24, (int)Math.Round(28 + ((int)scale - 100) * 0.12));

    private static StyleBoxFlat Surface(float shade) => new()
    {
        BgColor = shade == 0 ? Colors.Transparent : new Color(shade * 0.65f, shade, shade * 1.2f),
        ContentMarginLeft = 8, ContentMarginRight = 8,
        ContentMarginTop = 4, ContentMarginBottom = 4,
        CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
        CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
    };
}
