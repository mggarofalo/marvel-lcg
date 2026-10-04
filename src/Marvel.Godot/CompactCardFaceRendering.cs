using Godot;
using Marvel.View;

using static Marvel.Godot.CardValueRendering;

namespace Marvel.Godot;

/// <summary>Constructs bounded table and hand faces without inspector-only regions.</summary>
internal static class CompactCardFaceRendering
{
    internal static VBoxContainer Create(
        BoardCardPresentation card,
        CardDisplaySize size,
        CardLayoutMetrics layout,
        InterfaceScale scale)
    {
        var content = Stack();
        content.Name = "CardFace";
        CardStatusOverlay.AddTo(content, card);
        Label title = Label(
            card.Title, GodotThemeVariations.CardTitle, "Title", wrap: true, maximumLines: 2);
        // Godot asks wrapped labels for their minimum height before their
        // parent has assigned a width. Supplying the authored card width keeps
        // a short title from measuring as one character per line and stretching
        // a physical card through the table.
        title.CustomMinimumSize = new Vector2(
            Math.Max(1, layout.Width - 32), VisualSystem.Type(scale).Heading * 2 + 4);
        content.AddChild(title);
        if (size is CardDisplaySize.Hand or CardDisplaySize.Mulligan)
        {
            string kind = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(card.Kind.ToLowerInvariant());
            string identity = string.IsNullOrWhiteSpace(card.Classification)
                ? kind : $"{kind} · {card.Classification}";
            content.AddChild(Label(identity, GodotThemeVariations.Caption, "Kind"));
        }

        IReadOnlyList<BoardFieldPresentation> values = CompactValues(card, size);
        BoardFieldPresentation? schemeThreat = SchemeThreatBadge.Add(content, card, values);
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
        BoardFieldPresentation[] progress = [.. values.Where(value =>
            IsCompactProgressValue(value) && value != schemeThreat)];
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
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
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
