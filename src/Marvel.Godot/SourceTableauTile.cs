using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Compact B1 title and state strip; engine-named actions share its lower edge.</summary>
internal static class SourceTableauTile
{
    internal static CardControl CreateCard(BoardCardPresentation card, float width, InterfaceScale scale)
    {
        CardControl control = CardControl.CreateSource(card, width, scale);
        using var paper = new StyleBoxFlat { BgColor = CardFaceStyle.Paper, BorderColor = CardFaceStyle.Ink,
            BorderWidthBottom = 1, ContentMarginLeft = 2, ContentMarginRight = 2,
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
        button.AddThemeFontSizeOverride("font_size", 11);
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
        frame.AddThemeConstantOverride("separation", 1);
        var titlePanel = new PanelContainer { Name = "SourceTitleBand", MouseFilter = Control.MouseFilterEnum.Ignore };
        using var ink = new StyleBoxFlat { BgColor = CardFaceStyle.Ink, ContentMarginLeft = 6,
            ContentMarginRight = 6, ContentMarginTop = 1, ContentMarginBottom = 1 };
        titlePanel.AddThemeStyleboxOverride("panel", ink);
        frame.AddChild(titlePanel);
        Label title = CardSourceStrip.Label(card.Title, "SourceTitle", 13, CardTypography.Title);
        title.AutowrapMode = TextServer.AutowrapMode.Word;
        title.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
        titlePanel.AddChild(title);
        var stateRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        frame.AddChild(stateRow);
        Label type = CardSourceStrip.Label(card.Kind, "SourceType", 9, CardTypography.Body);
        type.AutowrapMode = TextServer.AutowrapMode.Off;
        stateRow.AddChild(type);
        Label state = CardSourceStrip.Label(State(card), "SourceState", 9, CardTypography.Body);
        state.AutowrapMode = TextServer.AutowrapMode.Off;
        state.AddThemeColorOverride("font_color", ClientTheme.ToGodot(CardVisualTokens.Secondary));
        stateRow.AddChild(state);
        return frame;
    }

    internal static string State(BoardCardPresentation card) => string.Join(" · ",
        new[] { SpatialTableZones.IsExhausted(card) ? "Exhausted" : "Ready" }
            .Concat(card.Counters.Select(value => $"{value.Value} {value.Name.ToLowerInvariant()}"))
            .Concat(card.Damage > 0 ? [$"{card.Damage} damage"] : []));
}
