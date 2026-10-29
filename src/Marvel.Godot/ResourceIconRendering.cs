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
        XNamespace ns = "http://www.w3.org/2000/svg";
        var image = new XElement(ns + "svg", new XAttribute("width", Size * glyphs.Length),
            new XAttribute("height", Size), new XAttribute("viewBox", $"0 0 {Size * glyphs.Length} {Size}"));
        for (int index = 0; index < glyphs.Length; index++)
        {
            using Stream source = typeof(ResourceIconRendering).Assembly.GetManifestResourceStream(
                $"Marvel.Godot.Assets.Resources.{glyphs[index]}.svg")
                ?? throw new InvalidOperationException("The canonical resource icon is unavailable.");
            using var reader = new StreamReader(source);
            XElement icon = XElement.Parse(ColoredOutline(reader.ReadToEnd(), glyphs[index]));
            image.Add(Position(icon, index));
        }
        DpiTexture texture = DpiTexture.CreateFromString(image.ToString(SaveOptions.DisableFormatting));
        texture.FixAlphaBorder = true;
        textures.Add(glyphs, texture);
        return texture;
    }

    private static XElement Position(XElement icon, int index)
    {
        float[] box = icon.Attribute("viewBox")!.Value.Split(' ')
            .Select(value => float.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        float factor = Size / Math.Max(box[2], box[3]);
        float left = index * Size + (Size - box[2] * factor) / 2;
        float top = (Size - box[3] * factor) / 2;
        string transform = FormattableString.Invariant(
            $"translate({left} {top}) scale({factor}) translate({-box[0]} {-box[1]})");
        return new XElement(icon.Name.Namespace + "g", new XAttribute("transform", transform), icon.Elements());
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
        button.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
        button.IconAlignment = HorizontalAlignment.Right;
        button.TooltipText = string.Join(" · ", new[]
        {
            button.TooltipText, CardRulesMarkup.ResourceNames(resources),
        }.Where(text => text.Length > 0));
    }

    internal static void ButtonContent(Button button, Control content, string accessibleName, Control? caption = null)
    {
        button.AccessibilityName = accessibleName;
        button.TooltipText = string.Empty;
        ButtonContentTypography.Apply(caption ?? content);
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
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TooltipText = CardRulesMarkup.ResourceNames(resources),
        });
        return row;
    }
}
