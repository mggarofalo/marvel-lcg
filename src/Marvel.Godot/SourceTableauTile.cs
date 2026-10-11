using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Compact B1 title and state strip; engine-named actions share its lower edge.</summary>
internal static class SourceTableauTile
{
    internal static CardControl CreateCard(BoardCardPresentation card, float width, InterfaceScale scale)
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
        Control summary = Create(card, width);
        body.AddChild(summary);
        body.MoveChild(summary, 0);
        SourceTileMeaning.Add(body, card);
        control.CustomMinimumSize = new Vector2(width, SourceTableauLayout.TileHeight);
        control.Size = control.CustomMinimumSize;
        control.SetMeta("source_tableau_tile", true);
        control.SetMeta("source_title", card.Title);
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

    internal static Control Create(BoardCardPresentation card, float width)
    {
        var frame = new VBoxContainer { Name = "SourceTileBody", MouseFilter = Control.MouseFilterEnum.Ignore };
        frame.AddThemeConstantOverride("separation", 0);
        var titlePanel = new Control { Name = "SourceTitleBand", CustomMinimumSize = new Vector2(0, 32),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        titlePanel.Draw += () => DrawHeader(titlePanel, card);
        titlePanel.Resized += titlePanel.QueueRedraw;
        frame.AddChild(titlePanel);
        Label title = CardSourceStrip.Label(card.Title, "SourceTitle", width < 220 ? 14 : 16, CardTypography.Title);
        title.Uppercase = true;
        title.AutowrapMode = TextServer.AutowrapMode.Off;
        title.ClipText = true;
        title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        title.VerticalAlignment = VerticalAlignment.Center;
        title.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
        titlePanel.AddChild(title);
        title.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        title.OffsetLeft = 7;
        title.OffsetRight = -30;
        var stateRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        stateRow.CustomMinimumSize = new Vector2(0, 19);
        stateRow.AddThemeConstantOverride("separation", 8);
        frame.AddChild(stateRow);
        Label type = CardSourceStrip.Label(card.Kind, "SourceType", 10, CardTypography.Bold);
        type.AutowrapMode = TextServer.AutowrapMode.Off;
        stateRow.AddChild(type);
        Label state = CardSourceStrip.Label(State(card), "SourceState", 10, CardTypography.Body);
        state.AutowrapMode = TextServer.AutowrapMode.Off;
        state.AddThemeColorOverride("font_color", ClientTheme.ToGodot(CardVisualTokens.Secondary));
        stateRow.AddChild(state);
        return frame;
    }

    private static void DrawHeader(Control header, BoardCardPresentation card)
    {
        float width = header.Size.X;
        Vector2[] ink = [Vector2.Zero, new(width - 8, 0), new(width, 8), new(width, 28), new(0, 32)];
        header.DrawColoredPolygon(ink, CardFaceStyle.Ink);
        CardEdgeSmoothing.Draw(header, ink, CardFaceStyle.Ink);
        Vector2[] accent = [new(width - 52, 0), new(width - 8, 0), new(width, 8), new(width, 28), new(width - 61, 30)];
        header.DrawColoredPolygon(accent, CardFaceStyle.Accent(card));
        CardEdgeSmoothing.Draw(header, accent, CardFaceStyle.Accent(card));
    }

    internal static string State(BoardCardPresentation card) => string.Join(" · ",
        new[] { SpatialTableZones.IsExhausted(card) ? "Exhausted" : "Ready" }
            .Concat(card.Counters.Select(value => $"{value.Value} {value.Name.ToLowerInvariant()}"))
            .Concat(card.Damage > 0 ? [$"{card.Damage} damage"] : []));
}
