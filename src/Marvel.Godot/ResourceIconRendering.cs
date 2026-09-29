using Godot;

namespace Marvel.Godot;

/// <summary>Uses the same canonical resource shapes in buttons, menus and card faces.</summary>
internal static class ResourceIconRendering
{
    private const int Size = 28;
    private static readonly Dictionary<string, Texture2D> textures = [];

    internal static Texture2D? Texture(string resources)
    {
        string glyphs = string.Concat(CardRulesMarkup.ResourceGlyphs(resources));
        if (glyphs.Length == 0) return null;
        if (textures.TryGetValue(glyphs, out Texture2D? cached)) return cached;
        using var image = Image.CreateEmpty(Size * glyphs.Length, Size, false, Image.Format.Rgba8);
        for (int index = 0; index < glyphs.Length; index++)
        {
            using Stream source = typeof(ResourceIconRendering).Assembly.GetManifestResourceStream(
                $"Marvel.Godot.Assets.Resources.{glyphs[index]}.svg")
                ?? throw new InvalidOperationException("The canonical resource icon is unavailable.");
            using var reader = new StreamReader(source);
            using var icon = new Image();
            if (icon.LoadSvgFromString(reader.ReadToEnd()) != Error.Ok)
                throw new InvalidOperationException("The canonical resource icon could not be decoded.");
            icon.Resize(Size, Size);
            image.BlitRect(icon, new Rect2I(0, 0, Size, Size), new Vector2I(index * Size, 0));
        }
        Texture2D texture = ImageTexture.CreateFromImage(image);
        textures.Add(glyphs, texture);
        return texture;
    }

    internal static void Apply(Button button, string resources)
    {
        button.Icon = Texture(resources);
        button.IconAlignment = HorizontalAlignment.Right;
        button.TooltipText = string.Join(" · ", new[]
        {
            button.TooltipText, CardRulesMarkup.ResourceNames(resources),
        }.Where(text => text.Length > 0));
    }

    internal static void ButtonContent(Button button, Control content, string accessibleName)
    {
        button.AccessibilityName = accessibleName;
        button.TooltipText = accessibleName;
        var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        margin.AddChild(content);
        button.AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        CardInspectorFocus.IgnoreMouseRecursively(content, interactiveRules: false);
        button.CustomMinimumSize = new Vector2(button.CustomMinimumSize.X,
            Math.Max(button.CustomMinimumSize.Y, content.GetCombinedMinimumSize().Y + 12));
    }

    internal static HBoxContainer Row(string text, string resources, string variation)
    {
        var row = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        Label label = DecisionPanel.Text(text, variation, wrap: true);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.CustomMinimumSize = new Vector2(80, 0);
        row.AddChild(label);
        row.AddChild(new TextureRect
        {
            Name = "ResourceSymbols", Texture = Texture(resources),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TooltipText = CardRulesMarkup.ResourceNames(resources),
        });
        return row;
    }
}
