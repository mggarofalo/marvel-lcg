using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Explicit synthetic specimen for native packaged-font and primitive review.</summary>
internal static class CardVisualSample
{
    internal static bool TryStart(Control owner)
    {
        if (!OS.GetCmdlineUserArgs().Contains("--marvel-b1-sample", StringComparer.Ordinal)) return false;
        owner.SetProcessInput(false);
        try
        {
            VerifyFonts();
            VerifyResources();
            Build(owner);
            GD.Print("B1_PRIMITIVES_OK synthetic=true");
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            owner.GetTree().Quit(1);
        }
        return true;
    }

    private static void Build(Control owner)
    {
        foreach (Node child in owner.GetChildren())
            if (child is CanvasItem item) item.Hide();
        var backdrop = new ColorRect { Color = ClientTheme.ToGodot(CardVisualTokens.Table),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        owner.AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var column = new VBoxContainer { Position = new Vector2(32, 24), Theme = ClientTheme.Create() };
        column.AddThemeConstantOverride("separation", 16);
        owner.AddChild(column);
        column.AddChild(Label("Impact Editions — native primitives", CardTypography.Title, 32));
        column.AddChild(Label("Synthetic visual specimen · frame geometry is reviewed separately", CardTypography.Body, 18));
        AddResources(column, CardVisualTokens.Paper, CardVisualTokens.Ink);
        AddResources(column, CardVisualTokens.Ink, CardVisualTokens.Paper);
        var scales = new HBoxContainer();
        column.AddChild(scales);
        var cards = new HBoxContainer { Name = "B1Cards" };
        cards.AddThemeConstantOverride("separation", 16);
        column.AddChild(cards);
        foreach (InterfaceScale scale in new[] { InterfaceScale.Percent50, InterfaceScale.Percent80,
                     InterfaceScale.Percent100, InterfaceScale.Percent150 })
        {
            var button = new Button { Name = $"B1Scale{(int)scale}", Text = $"{(int)scale}%", CustomMinimumSize = new Vector2(90, 44) };
            scales.AddChild(button);
            button.Pressed += () => ShowCards(cards, scale);
        }
        ShowCards(cards, ClientTheme.ConfiguredScale());
    }

    private static void ShowCards(HBoxContainer row, InterfaceScale scale)
    {
        foreach (Node child in row.GetChildren()) { row.RemoveChild(child); child.QueueFree(); }
        foreach (string kind in new[] { "HERO", "ALLY", "MINION", "MAIN SCHEME", "TREACHERY" })
        {
            var card = new BoardCardPresentation(1, 1, false, "A long title for measured wrapping", "", kind, "", [])
            {
                Classification = kind == "ALLY" ? "JUSTICE" : "",
                Cost = kind == "ALLY" ? "3" : null,
                FaceId = "synthetic",
                PrintedStats = [new("RES", "RBYW")],
                Traits = ["SYNTHETIC"],
                RulesMarkup = "<b>Visual sample.</b> Canonical [physical] [energy] [mental] [wild]. Separate [unique] [star] [cost].",
            };
            row.AddChild(CardControl.Create(card, CardDisplaySize.Board, scale));
        }
        row.SetMeta("sample_scale", (int)scale);
        GD.Print($"B1_SAMPLE_SCALE {(int)scale}");
    }

    private static void AddResources(VBoxContainer column, VisualColor field, VisualColor ink)
    {
        var panel = new PanelContainer();
        using var style = new StyleBoxFlat { BgColor = ClientTheme.ToGodot(field),
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 12, ContentMarginBottom = 12 };
        panel.AddThemeStyleboxOverride("panel", style);
        column.AddChild(panel);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 18);
        panel.AddChild(row);
        foreach (char glyph in "PEMW")
        {
            var label = Label(glyph.ToString(), CardRulesMarkup.ResourceFont(), 36);
            ResourceIconRendering.Style(label, glyph);
            row.AddChild(label);
        }
        var marks = Label("D S U", CardRulesMarkup.ResourceFont(), 36);
        marks.AddThemeColorOverride("font_color", ClientTheme.ToGodot(ink));
        row.AddChild(marks);
        var textures = new TextureRect { Texture = ResourceIconRendering.Texture("RYBW"),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
        row.AddChild(textures);
        var art = new TextureRect { Texture = BuiltInCardArt.Instance.Find("01040a"),
            CustomMinimumSize = new Vector2(72, 60), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
        row.AddChild(art);
    }

    private static Label Label(string text, Font font, int size)
    {
        var label = new Label { Text = text };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", ClientTheme.ToGodot(CardVisualTokens.Ink));
        return label;
    }

    private static void VerifyResources()
    {
        using Image image = ResourceIconRendering.Texture("RYBW")!.GetImage();
        const string glyphs = "PEMW";
        for (int index = 0; index < glyphs.Length; index++)
        {
            Color fill = ClientTheme.ToGodot(CardVisualTokens.Resource(glyphs[index]));
            bool foundFill = false, foundOutline = false;
            for (int y = 0; y < image.GetHeight(); y++)
                for (int x = index * CardVisualTokens.ResourceSize; x < (index + 1) * CardVisualTokens.ResourceSize; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    foundFill |= pixel.IsEqualApprox(fill);
                    Color outline = CardFaceStyle.Ink;
                    foundOutline |= pixel.A > 0.2f && Math.Abs(pixel.R - outline.R)
                        + Math.Abs(pixel.G - outline.G) + Math.Abs(pixel.B - outline.B) < 0.15f;
                }
            if (!foundFill || !foundOutline)
                throw new InvalidOperationException($"Resource {glyphs[index]} lost its semantic fill ({foundFill}) or contrasting outline ({foundOutline}).");
        }
    }

    private static void VerifyFonts()
    {
        foreach (Font font in new[] { CardTypography.Title, CardTypography.Body, CardTypography.Bold, CardTypography.Italic })
        {
            foreach (char character in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789—é")
                if (!font.HasChar(character)) throw new InvalidOperationException($"Missing text glyph {character}");
            if (font.GetAscent(18) <= 0 || font.GetDescent(18) <= 0)
                throw new InvalidOperationException("Packaged font has invalid baseline metrics.");
        }
        foreach (char glyph in "PEMWDSUBGAFCH")
            if (!CardRulesMarkup.ResourceFont().HasChar(glyph))
                throw new InvalidOperationException($"Missing canonical symbol {glyph}");
        foreach (int size in new[] { 14, 18, 29 })
        {
            Vector2 measured = CardTypography.Title.GetMultilineStringSize(
                "A long title for measured wrapping", HorizontalAlignment.Left, 156, size);
            GD.Print($"B1_FONT_METRICS title={size} width={measured.X} height={measured.Y} ascent={CardTypography.Title.GetAscent(size)} descent={CardTypography.Title.GetDescent(size)}");
        }
    }
}
