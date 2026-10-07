using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Builds a card face with fixed visual anchors instead of a growing metadata stack.</summary>
internal static class PrintedCardFace
{
    internal static Control Create(BoardCardPresentation card, CardLayoutMetrics layout,
        InterfaceScale scale, ICardArtProvider? art, CardDisplaySize displaySize)
    {
        Vector2 size = new(layout.Width - 8, layout.MinimumHeight - 8);
        var face = new Control { Name = "CardFace", CustomMinimumSize = size,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        bool landscape = VisualSystem.CardFrame(card.Kind).Family == CardFrameFamily.Scheme;
        Texture2D? illustration = displaySize == CardDisplaySize.Full && card.FaceId is { } id
            ? art?.Find(id) : null;
        IReadOnlyList<CardStatValue> stats = CardStatValues.From(card);
        var regions = new CardFaceRegions(size, new CardFaceFeatures(landscape, card.Cost is not null || card.PrintedStats.Any(value => value.Name == "Stage"),
            illustration is not null, stats.Count > 0, card.Traits.Count > 0, displaySize == CardDisplaySize.Full, CardStatusTokens.RowCount(card)));
        PrintedCardHeader.Add(face, card, regions);
        AddIllustration(face, card, regions, illustration);
        PrintedCardStats.Add(face, stats, regions);
        AddRules(face, card, regions, scale);
        CardFaceTokens.Add(face, card, regions);
        return face;
    }

    private static void AddIllustration(Control face, BoardCardPresentation card,
        CardFaceRegions r, Texture2D? texture)
    {
        if (texture is null) return;
        var well = Panel("IllustrationRegion", r.Illustration, CardFaceStyle.Accent(card).Darkened(0.6f));
        face.AddChild(well);
        if (texture is not null)
        {
            var image = new TextureRect { Name = "Illustration", Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = Control.MouseFilterEnum.Ignore };
            well.AddChild(image);
            image.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
    }

    private static void AddRules(Control face, BoardCardPresentation card, CardFaceRegions r, InterfaceScale scale)
    {
        var paper = Panel("RulesField", new Rect2(r.Traits.Position,
            new Vector2(r.Rules.Size.X, r.Rules.End.Y - r.Traits.Position.Y)), CardFaceStyle.Paper);
        face.AddChild(paper);
        Label traits = Text(string.Join(". ", card.Traits), "Traits", r.Traits, 18 * r.Unit);
        traits.HorizontalAlignment = HorizontalAlignment.Center;
        if (card.Traits.Count > 0) face.AddChild(traits);
        else traits.Free();
        CardRulesMarkup.ResourceFont();
        var rules = new RichTextLabel
        {
            Name = "RulesText", Position = r.Rules.Position + new Vector2(8, 2) * r.Unit,
            Size = r.Rules.Size - new Vector2(16, 4) * r.Unit,
            BbcodeEnabled = true, Text = CardRulesMarkup.ToBbCode(card.RulesMarkup, card.RulesText, scale,
                Mathf.RoundToInt(r.RulesFontSize * 1.22f)),
            ScrollActive = false, FitContent = false, MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rules.AddThemeFontOverride("normal_font", CardTypography.Body);
        rules.AddThemeFontOverride("bold_font", CardTypography.Bold);
        rules.AddThemeFontOverride("italics_font", CardTypography.Italic);
        rules.AddThemeColorOverride("default_color", CardFaceStyle.Ink);
        foreach (string font in new[] { "normal_font_size", "bold_font_size", "italics_font_size" })
            rules.AddThemeFontSizeOverride(font, Math.Max(5, Mathf.RoundToInt(r.RulesFontSize)));
        face.AddChild(rules);
    }

    internal static Label Text(string text, string name, Rect2 bounds, float fontSize)
    {
        var label = new Label { Name = name,
            MouseFilter = Control.MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, ClipText = true };
        label.AddThemeFontOverride("font", CardTypography.Title);
        label.AddThemeFontSizeOverride("font_size", Math.Max(5, Mathf.RoundToInt(fontSize)));
        label.AddThemeColorOverride("font_color", CardFaceStyle.Ink);
        label.Text = text;
        label.Position = bounds.Position;
        label.Size = bounds.Size;
        return label;
    }

    internal static Panel Panel(string name, Rect2 bounds, Color color)
    {
        var panel = new Panel { Name = name, Position = bounds.Position, Size = bounds.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        using var style = new StyleBoxFlat { BgColor = color,
            BorderColor = CardFaceStyle.Ink, BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderWidthTop = 1, BorderWidthBottom = 1, ContentMarginLeft = 0,
            ContentMarginRight = 0, ContentMarginTop = 0, ContentMarginBottom = 0 };
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

}
