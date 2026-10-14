using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Compact B1 title and state strip; engine-named actions share its lower edge.</summary>
internal static class SourceTableauTile
{
    internal static CardControl CreateCard(BoardCardPresentation card, float width, InterfaceScale scale, string copy = "", bool compact = false)
    {
        CardControl control = CardControl.CreateSource(card, width, scale);
        using var paper = new StyleBoxFlat { BgColor = CardFaceStyle.Paper, BorderColor = CardFaceStyle.Accent(card),
            BorderWidthLeft = 4, BorderWidthBottom = 1, ContentMarginLeft = 4, ContentMarginRight = 1,
            ContentMarginTop = 2, ContentMarginBottom = 2 };
        control.AddThemeStyleboxOverride("panel", paper);
        var body = control.GetNode<VBoxContainer>("SourceBody");
        body.AddThemeConstantOverride("separation", 0);
        Node strip = body.GetNode("SourceStrip");
        body.RemoveChild(strip);
        strip.QueueFree();
        Control summary = SourceTileHeader.Create(card, copy);
        body.AddChild(summary);
        body.MoveChild(summary, 0);
        if (!compact) SourceTileMeaning.Add(body, card);
        control.CustomMinimumSize = new Vector2(width, compact ? 32 : SourceTableauLayout.TileHeight);
        control.Size = control.CustomMinimumSize;
        control.SetMeta("source_tableau_tile", true);
        control.SetMeta("source_title", card.Title);
        control.SetMeta("source_copy", copy);
        control.SetMeta("source_identity", copy.Length > 0 ? $"{card.Title}, {copy}" : card.Title);
        control.AccessibilityName += copy.Length > 0 ? $". {copy}" : "";
        return control;
    }

    internal static void StyleAction(Button button, string title)
    {
        button.Text = button.AccessibilityName;
        button.SelfModulate = Colors.White;
        button.CustomMinimumSize = new Vector2(0, 28);
        button.AutowrapMode = TextServer.AutowrapMode.Word;
        button.Alignment = HorizontalAlignment.Left;
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.AddThemeFontSizeOverride("font_size", 12);
        button.AddThemeFontOverride("font", CardTypography.Bold);
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
        {
            bool unavailable = state == "disabled";
            Color fill = state switch
            {
                "hover" => CardFaceStyle.Ink.Lightened(0.18f),
                "pressed" or "hover_pressed" => CardFaceStyle.Ink.Darkened(0.25f),
                "disabled" => CardFaceStyle.Paper.Darkened(0.08f),
                _ => CardFaceStyle.Ink,
            };
            using var treatment = new StyleBoxFlat { BgColor = fill,
                BorderColor = CardFaceStyle.Ink with { A = 0.18f }, BorderWidthTop = 1,
                BorderWidthBottom = state is "hover" ? 2 : 1,
                ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 0, ContentMarginBottom = 0 };
            button.AddThemeStyleboxOverride(state, treatment);
            button.AddThemeColorOverride(state == "normal" ? "font_color" : $"font_{state}_color", unavailable ? ClientTheme.ToGodot(CardVisualTokens.Secondary) : CardFaceStyle.Paper);
        }
        using var focus = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = ClientTheme.ToGodot(CardVisualTokens.Modified),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2 };
        button.AddThemeStyleboxOverride("focus", focus);
        button.AddThemeColorOverride("font_focus_color", CardFaceStyle.Paper);
    }

    internal static string State(BoardCardPresentation card) => string.Join(" · ",
        new[] { SpatialTableZones.IsExhausted(card) ? "Exhausted" : "Ready" }
            .Concat(card.Counters.Select(value => $"{value.Value} {value.Name.ToLowerInvariant()}"))
            .Concat(card.DamageKnown && card.Damage > 0 ? [$"{card.Damage} damage"] : []));
}
