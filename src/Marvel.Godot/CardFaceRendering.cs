using Godot;
using Marvel.View;

using static Marvel.Godot.CardValueRendering;

namespace Marvel.Godot;

/// <summary>Constructs full, compact, and concealed card faces.</summary>
internal static class CardFaceRendering
{
    internal static Control CreateBody(
        BoardCardPresentation card,
        CardDisplaySize size,
        CardLayoutMetrics layout,
        InterfaceScale scale,
        ICardArtProvider? art) =>
        card.Concealed
            ? Back(card)
            : size == CardDisplaySize.Full
                ? FullFace(card, layout, scale, art)
                : CompactFace(card, size, scale);

    internal static VBoxContainer Back(BoardCardPresentation card)
    {
        var content = Stack();
        content.Name = "CardBack";
        content.AddChild(Label("Card back", GodotThemeVariations.Eyebrow, "BackKind"));
        content.AddChild(Label(
            string.IsNullOrWhiteSpace(card.Back) ? "Concealed" : card.Back,
            GodotThemeVariations.CardTitle,
            "BackIdentity",
            wrap: true));
        content.AddChild(Label(
            card.Count == 1 ? "Identity hidden" : $"{card.Count} cards · order hidden",
            GodotThemeVariations.MutedText,
            "BackCount",
            wrap: true));
        return content;
    }

    internal static VBoxContainer FullFace(
        BoardCardPresentation card,
        CardLayoutMetrics layout,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        CardFrameProfile profile = VisualSystem.CardFrame(card.Kind);
        var content = Stack();
        content.Name = "CardFace";
        BoardFieldPresentation? primary = PrimaryValue(card, profile.Family);
        content.AddChild(Header(card, profile.Family, primary));
        content.AddChild(ArtRegion(card, profile.Family, layout, art));
        AddTraits(content, card);

        BoardFieldPresentation? schemeThreat = profile.Family == CardFrameFamily.Scheme
            ? SchemeThreat(card)
            : null;
        AddPrintedValues(content, card, profile.Family, primary, schemeThreat);
        content.AddChild(RulesText(card, scale));
        AddLiveValues(content, card, profile.Family, schemeThreat);
        AddResourceIcons(content, card, scale);
        return content;
    }

    private static HBoxContainer Header(
        BoardCardPresentation card,
        CardFrameFamily family,
        BoardFieldPresentation? primary)
    {
        var header = new HBoxContainer
        {
            Name = "Masthead",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        if (primary is not null && family != CardFrameFamily.Scheme)
        {
            header.AddChild(Badge(primary, "PrimaryValue"));
        }
        header.AddChild(Identity(card));
        if (primary is not null && family == CardFrameFamily.Scheme)
        {
            header.AddChild(Badge(primary, "PrimaryValue"));
        }
        if (!string.IsNullOrWhiteSpace(card.Status))
        {
            header.AddChild(Badge(
                new BoardFieldPresentation("STATE", card.Status), "ReadyIndicator"));
        }
        return header;
    }

    private static VBoxContainer Identity(BoardCardPresentation card)
    {
        var identity = Stack();
        identity.Name = "Identity";
        identity.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        identity.AddChild(Label(card.Title, GodotThemeVariations.CardTitle, "Title", wrap: true));
        if (!string.IsNullOrWhiteSpace(card.Subtitle))
        {
            identity.AddChild(Label(
                card.Subtitle, GodotThemeVariations.MutedText, "Subtitle", wrap: true));
        }
        identity.AddChild(Label(card.Kind, GodotThemeVariations.Eyebrow, "Kind", wrap: true));
        return identity;
    }

    private static PanelContainer ArtRegion(
        BoardCardPresentation card,
        CardFrameFamily family,
        CardLayoutMetrics layout,
        ICardArtProvider? art)
    {
        var region = new PanelContainer
        {
            Name = "IllustrationRegion",
            ThemeTypeVariation = GodotThemeVariations.CardArtWell,
            CustomMinimumSize = new Vector2(
                0,
                family == CardFrameFamily.Scheme
                    ? layout.MinimumHeight * 0.20f
                    : layout.Width * 0.32f),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        Texture2D? illustration = card.FaceId is { Length: > 0 } faceId
            ? art?.Find(faceId)
            : null;
        if (illustration is not null)
        {
            region.AddChild(new TextureRect
            {
                Name = "Illustration",
                Texture = illustration,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
        }
        return region;
    }

    private static void AddTraits(VBoxContainer content, BoardCardPresentation card)
    {
        if (card.Traits.Count > 0)
        {
            content.AddChild(Label(
                string.Join("  ·  ", card.Traits),
                GodotThemeVariations.Caption,
                "Traits",
                wrap: true));
        }
    }

    private static void AddPrintedValues(
        VBoxContainer content,
        BoardCardPresentation card,
        CardFrameFamily family,
        BoardFieldPresentation? primary,
        BoardFieldPresentation? schemeThreat)
    {
        List<BoardFieldPresentation> printed = card.PrintedStats
            .Where(value => value.Name is not "RES")
            .Where(value => primary is null || value.Name != primary.Name)
            .Where(value => family != CardFrameFamily.Identity
                || value.Name != "HP"
                || !card.Fields.Any(field => field.Name == "HEALTH"))
            .Where(value => family != CardFrameFamily.Scheme || value.Name is not "TargetThreat")
            .ToList();
        if (schemeThreat is not null)
        {
            printed.Add(schemeThreat);
        }
        if (printed.Count > 0)
        {
            content.AddChild(ValueStrip(
                family == CardFrameFamily.Scheme ? "" : "PRINTED",
                printed, GodotThemeVariations.CardPrintedValue, "PrintedValues",
                horizontal: true));
        }
    }

    private static RichTextLabel RulesText(BoardCardPresentation card, InterfaceScale scale)
    {
        CardRulesMarkup.ResourceFont();
        return new RichTextLabel
        {
            Name = "RulesText",
            BbcodeEnabled = true,
            Text = CardRulesMarkup.ToBbCode(card.RulesMarkup, card.RulesText, scale),
            FitContent = true,
            ScrollActive = false,
            FocusMode = Control.FocusModeEnum.All,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            CustomMinimumSize = Vector2.Zero,
            ThemeTypeVariation = GodotThemeVariations.CardRulesRich,
        };
    }

    private static void AddLiveValues(
        VBoxContainer content,
        BoardCardPresentation card,
        CardFrameFamily family,
        BoardFieldPresentation? schemeThreat)
    {
        if (family == CardFrameFamily.Scheme && schemeThreat is not null)
        {
            return;
        }
        List<BoardFieldPresentation> live = LiveValues(card);
        if (family == CardFrameFamily.Identity)
        {
            live = live.Where(value => value.Name == "HEALTH").ToList();
        }
        if (live.Count > 0)
        {
            content.AddChild(ValueStrip(
                "CURRENT", live, GodotThemeVariations.CardLiveValue, "LiveValues",
                horizontal: true));
        }
    }

    private static void AddResourceIcons(
        VBoxContainer content,
        BoardCardPresentation card,
        InterfaceScale scale)
    {
        BoardFieldPresentation? resource = card.PrintedStats
            .FirstOrDefault(value => value.Name == "RES");
        if (resource is not null)
        {
            content.AddChild(InspectorResourceValue(resource, scale));
        }
    }

    private static HBoxContainer InspectorResourceValue(
        BoardFieldPresentation resource,
        InterfaceScale scale)
    {
        var presentation = new HBoxContainer
        {
            Name = "ResourceIcons",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
        };

        Font iconFont = CardRulesMarkup.ResourceFont();
        int index = 0;
        foreach ((_, string glyph) in CardRulesMarkup.ResourceTokens(resource.Value))
        {
            ResourceIconMetrics metrics = VisualSystem.ResourceIcon(glyph, scale);
            Label slot = Label(
                glyph,
                GodotThemeVariations.CardPrintedValue,
                $"InspectorResourceIconSlot{index}");
            slot.CustomMinimumSize = new Vector2(metrics.SlotSize, metrics.SlotSize);
            slot.HorizontalAlignment = HorizontalAlignment.Center;
            slot.VerticalAlignment = VerticalAlignment.Center;
            slot.AddThemeFontOverride("font", iconFont);
            slot.AddThemeFontSizeOverride("font_size", metrics.FontSize);
            presentation.AddChild(slot);
            index++;
        }
        return presentation;
    }

    internal static VBoxContainer CompactFace(
        BoardCardPresentation card,
        CardDisplaySize size,
        InterfaceScale scale)
    {
        var content = Stack();
        content.Name = "CardFace";
        if (size == CardDisplaySize.Hand)
        {
            string identity = string.IsNullOrWhiteSpace(card.Classification)
                ? card.Kind
                : $"{card.Kind}  /  {card.Classification.ToUpperInvariant()}";
            content.AddChild(Label(identity, GodotThemeVariations.Eyebrow, "Kind"));
        }
        content.AddChild(Label(card.Title, GodotThemeVariations.CardTitle, "Title", wrap: true));

        IReadOnlyList<BoardFieldPresentation> values = CompactValues(card, size);
        BoardFieldPresentation[] summary = [.. values.Where(value =>
            !IsCompactProgressValue(value) && value.Name != "RES")];
        if (summary.Length > 0)
        {
            content.AddChild(ValueStrip(
                string.Empty,
                summary,
                GodotThemeVariations.CardLiveValue,
                "SummaryValues",
                horizontal: true));
        }
        foreach (BoardFieldPresentation resource in values.Where(value => value.Name == "RES"))
        {
            content.AddChild(ResourceValue(resource, scale));
        }
        BoardFieldPresentation[] progress = [.. values.Where(IsCompactProgressValue)];
        if (progress.Length > 0)
        {
            content.AddChild(ValueStrip(
                string.Empty,
                progress,
                GodotThemeVariations.CardLiveValue,
                "ProgressValues"));
        }

        if (CompactState(card, size) is { } state)
        {
            content.AddChild(Label(state, GodotThemeVariations.CardState, "StateStrip", wrap: true));
        }
        return content;
    }

    private static HFlowContainer ResourceValue(
        BoardFieldPresentation resource,
        InterfaceScale scale)
    {
        var presentation = new HFlowContainer
        {
            Name = "SummaryValuesRES",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
        };
        Font iconFont = CardRulesMarkup.ResourceFont();
        int index = 0;
        foreach ((_, string glyph) in CardRulesMarkup.ResourceTokens(resource.Value))
        {
            var token = new HBoxContainer
            {
                Name = $"ResourceToken{index}",
                ThemeTypeVariation = GodotThemeVariations.CompactRow,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            };
            ResourceIconMetrics metrics = VisualSystem.ResourceIcon(glyph, scale);
            Label slot = Label(
                glyph,
                GodotThemeVariations.CardLiveValue,
                $"ResourceIconSlot{index}");
            slot.CustomMinimumSize = new Vector2(metrics.SlotSize, metrics.SlotSize);
            slot.HorizontalAlignment = HorizontalAlignment.Center;
            slot.VerticalAlignment = VerticalAlignment.Center;
            slot.AddThemeFontOverride("font", iconFont);
            slot.AddThemeFontSizeOverride("font_size", metrics.FontSize);
            token.AddChild(slot);
            presentation.AddChild(token);
            index++;
        }
        return presentation;
    }
}
