using System.Globalization;
using System.Xml.Linq;
using Godot;

namespace Marvel.Godot;

/// <summary>Uses the same canonical resource shapes in buttons, menus and card faces.</summary>
internal static class ResourceIconRendering
{
    private const int Size = CardVisualTokens.ResourceSize;
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
            if (icon.LoadSvgFromString(ColoredOutline(reader.ReadToEnd(), glyphs[index])) != Error.Ok)
                throw new InvalidOperationException("The canonical resource icon could not be decoded.");
            icon.FixAlphaEdges();
            icon.Resize(Size, Size, Image.Interpolation.Lanczos);
            image.BlitRect(icon, new Rect2I(0, 0, Size, Size), new Vector2I(index * Size, 0));
        }
        image.FixAlphaEdges();
        image.GenerateMipmaps();
        Texture2D texture = ImageTexture.CreateFromImage(image);
        textures.Add(glyphs, texture);
        return texture;
    }

    internal static void Style(Label label, char glyph)
    {
        label.AddThemeFontOverride("font", CardRulesMarkup.ResourceFont());
        label.AddThemeColorOverride("font_color", ClientTheme.ToGodot(CardVisualTokens.Resource(glyph)));
        label.AddThemeColorOverride("font_outline_color", CardFaceStyle.Ink);
        label.AddThemeConstantOverride("outline_size", CardVisualTokens.ResourceOutline);
    }

    private static string ColoredOutline(string svg, char glyph)
    {
        XElement root = XElement.Parse(svg);
        float[] viewBox = root.Attribute("viewBox")!.Value.Split(' ')
            .Select(value => float.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        string stroke = (Math.Max(viewBox[2], viewBox[3]) / Size * CardVisualTokens.ResourceOutline).ToString(CultureInfo.InvariantCulture);
        foreach (XElement path in root.Descendants().Where(element => element.Name.LocalName == "path"))
        {
            path.SetAttributeValue("fill", "#" + ClientTheme.ToGodot(CardVisualTokens.Resource(glyph)).ToHtml(false));
            path.SetAttributeValue("stroke", "#" + CardFaceStyle.Ink.ToHtml(false));
            path.SetAttributeValue("stroke-width", stroke);
            path.SetAttributeValue("stroke-linejoin", "round");
            path.SetAttributeValue("paint-order", "stroke fill");
        }
        return root.ToString(SaveOptions.DisableFormatting);
    }

    internal static void Apply(Button button, string resources)
    {
        button.Icon = Texture(resources);
        button.TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps;
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
        ButtonContentLayout.Attach(button, content);
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
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TooltipText = CardRulesMarkup.ResourceNames(resources),
        });
        return row;
    }
}
