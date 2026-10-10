using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>B1 source typography and rails with a separate current-action row.</summary>
internal static class SourceTableauTile
{
    internal static CardControl CreateCard(BoardCardPresentation card, float width, InterfaceScale scale)
    {
        CardControl control = CardControl.CreateSource(card, width, scale);
        var body = control.GetNode<VBoxContainer>("SourceBody");
        body.AddThemeConstantOverride("separation", 0);
        Node strip = body.GetNode("SourceStrip");
        body.RemoveChild(strip);
        strip.QueueFree();
        Control summary = Create(card, width);
        body.AddChild(summary);
        body.MoveChild(summary, 0);
        summary.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        control.CustomMinimumSize = new Vector2(width, SourceTableauLayout.TileHeight);
        control.Size = control.CustomMinimumSize;
        control.SetMeta("source_tableau_tile", true);
        control.SetMeta("source_title", card.Title);
        return control;
    }

    internal static void StyleAction(Button button, string title)
    {
        button.Text = button.AccessibilityName == title ? $"Use {title}" : button.AccessibilityName;
        button.SelfModulate = Colors.White;
        button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.AddThemeFontSizeOverride("font_size", 12);
        button.AddThemeFontOverride("font", CardTypography.Bold);
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
        {
            bool active = state is "hover" or "pressed" or "hover_pressed";
            using var treatment = new StyleBoxFlat { BgColor = active ? CardFaceStyle.Ink : ClientTheme.ToGodot(CardVisualTokens.Table),
                BorderColor = CardFaceStyle.Ink with { A = 0.18f }, BorderWidthTop = 1,
                ContentMarginLeft = 6, ContentMarginRight = 6 };
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
        MarginContainer frame = CardSourceStrip.Frame(card, width, out VBoxContainer body);
        frame.Name = "SourceTileBody";
        CardSourceStrip.AddTitle(body, card.Title);
        var identity = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        body.AddChild(identity);
        var type = CardSourceStrip.Label(card.Kind, "SourceType", 9, CardTypography.Body);
        type.AutowrapMode = TextServer.AutowrapMode.Off;
        type.AddThemeColorOverride("font_color", ClientTheme.ToGodot(CardVisualTokens.Secondary));
        identity.AddChild(type);
        Label state = CardSourceStrip.Label(State(card), "SourceState", 10, CardTypography.Body);
        state.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        state.HorizontalAlignment = HorizontalAlignment.Right;
        state.AddThemeColorOverride("font_color", ClientTheme.ToGodot(CardVisualTokens.Secondary));
        identity.AddChild(state);
        CardSourceStrip.AddContributions(body, card, showRecipients: false);
        if (card.Persistent is { Contributions.Count: > 0 }) return frame;
        IReadOnlyList<string> lines = CardSourceSummary.Lines(card);
        bool printed = card.Persistent is { Abilities.Count: 0, HasUnresolvedAbilities: true };
        string meaning = lines.Count > 0 ? lines[0] : "";
        BoardCardPresentation preview = printed ? card : card with { RulesText = meaning, RulesMarkup = "" };
        RichTextLabel rules = CardRulesRendering.Create(preview, InterfaceScale.Standard, 11);
        rules.Name = "SourceMeaning";
        rules.CustomMinimumSize = new Vector2(0, 26);
        rules.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rules.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        body.AddChild(rules);
        SourceRuleExcerpt.Bind(rules);
        return frame;
    }

    internal static string State(BoardCardPresentation card) => string.Join(" · ",
        new[] { SpatialTableZones.IsExhausted(card) ? "Exhausted" : "Ready" }
            .Concat(card.Counters.Select(value => $"{value.Value} {value.Name.ToLowerInvariant()}"))
            .Concat(card.Damage > 0 ? [$"{card.Damage} damage"] : []));

}
