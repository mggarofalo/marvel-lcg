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
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.AddThemeFontSizeOverride("font_size", 12);
        button.AddThemeFontOverride("font", CardTypography.Bold);
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
        {
            bool active = state is "hover" or "pressed" or "hover_pressed";
            using var treatment = new StyleBoxFlat { BgColor = active ? CardFaceStyle.Ink : CardFaceStyle.Paper,
                BorderColor = CardFaceStyle.Ink with { A = 0.18f }, BorderWidthTop = 1,
                ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 0, ContentMarginBottom = 0 };
            button.AddThemeStyleboxOverride(state, treatment);
            button.AddThemeColorOverride(state == "normal" ? "font_color" : $"font_{state}_color", active ? CardFaceStyle.Paper : CardFaceStyle.Ink);
        }
        using var focus = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = ClientTheme.ToGodot(CardVisualTokens.Modified),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2 };
        button.AddThemeStyleboxOverride("focus", focus);
        button.AddThemeColorOverride("font_focus_color", CardFaceStyle.Ink);
    }

    internal static string State(BoardCardPresentation card) => string.Join(" · ",
        new[] { SpatialTableZones.IsExhausted(card) ? "Exhausted" : "Ready" }
            .Concat(card.Counters.Select(value => $"{value.Value} {value.Name.ToLowerInvariant()}"))
            .Concat(card.Damage > 0 ? [$"{card.Damage} damage"] : []));
}
