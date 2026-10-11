using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Builds a card face with fixed visual anchors instead of a growing metadata stack.</summary>
internal static class PrintedCardFace
{
    internal static Control Create(BoardCardPresentation card, CardLayoutMetrics layout,
        InterfaceScale scale, ICardArtProvider? art, CardDisplaySize displaySize)
    {
        Vector2 size = new(layout.Width - 2 * CardVisualTokens.FrameInset,
            layout.MinimumHeight - 2 * CardVisualTokens.FrameInset);
        var face = new Control { Name = "CardFace", CustomMinimumSize = size,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        bool landscape = VisualSystem.CardFrame(card.Kind).Family == CardFrameFamily.Scheme;
        Texture2D? illustration = card.FaceId is { } id
            ? art?.Find(id) ?? BuiltInCardArt.Instance.Find(id) : null;
        IReadOnlyList<CardStatValue> stats = CardStatValues.From(card);
        var features = new CardFaceFeatures(landscape, card.Cost is not null || card.PrintedStats.Any(value => value.Name == "Stage"),
            illustration is not null, stats.Count > 0, card.Traits.Count > 0,
            displaySize == CardDisplaySize.Full, CardPrintedIcons.RowCount(card))
        { HasConsequences = stats.Any(stat => stat.ConsequentialDamage > 0),
            HasProgress = CardProgressValue.From(card) is not null };
        var provisional = new CardFaceRegions(size, features);
        RichTextLabel rules = CardRulesRendering.Create(card, scale, provisional.RulesFontSize);
        rules.Size = new Vector2(provisional.Rules.Size.X, size.Y);
        face.AddChild(rules);
        CardFaceTokens.Add(face, card, provisional);
        face.Ready += () =>
        {
            var regions = new CardFaceRegions(size, features with
            {
                RulesHeight = rules.GetContentHeight(),
                TitleHeight = PrintedCardHeader.MeasureTitle(card, provisional, face),
            });
            face.SetMeta("measured_rules", rules.GetContentHeight());
            face.SetMeta("ink_end", regions.InkEnd);
            face.SetMeta("title_height", regions.Title.Size.Y);
            CardFacePlanes.Add(face, card, regions, size);
            PrintedCardHeader.Add(face, card, regions);
            AddIllustration(face, card, regions, illustration);
            PrintedCardStats.Add(face, stats, regions);
            AddRules(face, card, regions, rules);
            CardPrintedIcons.Add(face, card, regions);
        };
        return face;
    }

    private static void AddIllustration(Control face, BoardCardPresentation card,
        CardFaceRegions r, Texture2D? texture)
    {
        if (texture is null) return;
        if (r.Illustration.Size.Y <= 0) return;
        var well = new Control { Name = "IllustrationRegion", Position = r.Illustration.Position,
            Size = r.Illustration.Size, ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        face.AddChild(well);
        var image = new TextureRect { Name = "Illustration", Texture = texture,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        well.AddChild(image);
        image.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    private static void AddRules(Control face, BoardCardPresentation card, CardFaceRegions r, RichTextLabel rules)
    {
        Label traits = Text(string.Join(". ", card.Traits).ToUpperInvariant(), "Traits", r.Traits,
            (r.Full ? CardVisualTokens.FullTraitSize : CardVisualTokens.CompactTraitSize) * r.Density);
        traits.HorizontalAlignment = HorizontalAlignment.Right;
        traits.AddThemeFontOverride("font", CardTypography.Bold);
        if (card.Traits.Count > 0) face.AddChild(traits);
        else traits.Free();
        rules.Position = r.Rules.Position;
        rules.Size = r.Rules.Size;
    }

    internal static Label Text(string text, string name, Rect2 bounds, float fontSize)
    {
        var label = new Label { Name = name,
            MouseFilter = Control.MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, ClipText = true };
        label.AddThemeConstantOverride("line_spacing", 0);
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
