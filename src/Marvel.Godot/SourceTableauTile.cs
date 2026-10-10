using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Concise source facts preserve space for the current offered action.</summary>
internal static class SourceTableauTile
{
    internal static CardControl CreateCard(BoardCardPresentation card, float width, InterfaceScale scale)
    {
        CardControl control = CardControl.CreateSource(card, width, scale);
        var body = control.GetNode<VBoxContainer>("SourceBody");
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
        button.Text = button.AccessibilityName == title ? $"Use {title}" : button.AccessibilityName;
        button.SelfModulate = Colors.White;
        button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.AddThemeFontSizeOverride("font_size", 12);
        button.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
        using var available = new StyleBoxFlat { BgColor = CardFaceStyle.Ink,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3 };
        button.AddThemeStyleboxOverride("normal", available);
    }

    internal static Control Create(BoardCardPresentation card, float width)
    {
        var body = new VBoxContainer { Name = "SourceTileBody", CustomMinimumSize = new Vector2(width - 8, 50),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        body.AddThemeConstantOverride("separation", 1);
        Add(body, "SourceTitle", card.Title, 14);
        Add(body, "SourceState", State(card), 11);
        string contributions = string.Join(" · ", card.Persistent?.Contributions
            .Select(CardSourceSummary.Contribution).Distinct() ?? []);
        IReadOnlyList<string> lines = CardSourceSummary.Lines(card);
        bool printed = contributions.Length == 0 && card.Persistent is { Abilities.Count: 0, HasUnresolvedAbilities: true };
        string meaning = contributions.Length > 0 ? contributions : lines.Count > 0 ? lines[0] : "";
        BoardCardPresentation preview = printed ? card : card with { RulesText = meaning, RulesMarkup = "" };
        RichTextLabel rules = CardRulesRendering.Create(preview, InterfaceScale.Standard, 11);
        rules.Name = "SourceMeaning";
        rules.CustomMinimumSize = new Vector2(0, 26);
        rules.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddChild(rules);
        return body;
    }

    internal static string State(BoardCardPresentation card) => string.Join(" · ",
        new[] { SpatialTableZones.IsExhausted(card) ? "Exhausted" : "Ready" }
            .Concat(card.Counters.Select(value => $"{value.Value} {value.Name.ToLowerInvariant()}"))
            .Concat(card.Damage > 0 ? [$"{card.Damage} damage"] : []));

    private static void Add(VBoxContainer body, string name, string text, int fontSize)
    {
        var label = new Label { Name = name, Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore, TooltipText = text };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", CardFaceStyle.Ink);
        label.AddThemeFontOverride("font", name == "SourceTitle" ? CardTypography.Bold : CardTypography.Body);
        body.AddChild(label);
    }
}
