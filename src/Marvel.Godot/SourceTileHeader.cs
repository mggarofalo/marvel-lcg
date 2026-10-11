using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Native-size source identity and symbolic public state within a measured title band.</summary>
internal static class SourceTileHeader
{
    internal static Control Create(BoardCardPresentation card, string copy)
    {
        var panel = new VBoxContainer { Name = "SourceTitleBand", MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeConstantOverride("separation", 0);
        panel.Draw += () => Draw(panel, card);
        panel.Resized += panel.QueueRedraw;
        var inset = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        inset.AddThemeConstantOverride("margin_left", 7);
        inset.AddThemeConstantOverride("margin_right", 30);
        panel.AddChild(inset);
        Label title = CardSourceStrip.Label(card.Title, "SourceTitle", 16, CardTypography.Title);
        title.Uppercase = true;
        title.CustomMinimumSize = new Vector2(0, 38);
        title.VerticalAlignment = VerticalAlignment.Center;
        title.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
        inset.AddChild(title);
        var symbols = new HFlowContainer { Name = "SourceSymbols", MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(0, 16) };
        symbols.AddThemeConstantOverride("h_separation", 4);
        symbols.AddThemeConstantOverride("v_separation", 2);
        panel.AddChild(symbols);
        symbols.AddChild(Symbol(card.Kind == "SUPPORT" ? "support" : "upgrade", "SourceType", card.Kind));
        bool exhausted = SpatialTableZones.IsExhausted(card);
        symbols.AddChild(Symbol(exhausted ? "exhausted" : "ready", "SourceState", exhausted ? "Exhausted" : "Ready"));
        if (copy.Length > 0)
        {
            Label marker = CardSourceStrip.Label(copy, "SourceCopy", 10, CardTypography.Bold);
            marker.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
            marker.AutowrapMode = TextServer.AutowrapMode.Off;
            symbols.AddChild(marker);
        }
        for (int index = 0; index < card.Counters.Count; index++)
            symbols.AddChild(SourceCounterPips.Create(card.Counters[index], card.Counters.Count > 1));
        if (card.Damage > 0)
        {
            var damage = new HBoxContainer { AccessibilityName = $"{card.Damage} damage", TooltipText = $"{card.Damage} damage" };
            damage.AddChild(Symbol("heart", "SourceDamage", damage.AccessibilityName));
            Label count = CardSourceStrip.Label(card.Damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "SourceDamageValue", 10, CardTypography.Body);
            count.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
            damage.AddChild(count);
            symbols.AddChild(damage);
        }
        return panel;
    }

    private static Control Symbol(string glyph, string name, string description)
    {
        var icon = new Control { Name = name, CustomMinimumSize = new Vector2(16, 16),
            AccessibilityName = description, TooltipText = description, MouseFilter = Control.MouseFilterEnum.Pass };
        icon.AddChild(CardGlyphRendering.Create(glyph, "SourceSymbol", new Rect2(0, 0, 16, 16), CardFaceStyle.Paper));
        return icon;
    }

    private static void Draw(Control panel, BoardCardPresentation card)
    {
        float width = panel.Size.X;
        float height = panel.Size.Y;
        Vector2[] ink = [Vector2.Zero, new(width - 8, 0), new(width, 8), new(width, height), new(0, height)];
        panel.DrawColoredPolygon(ink, CardFaceStyle.Ink);
        CardEdgeSmoothing.Draw(panel, ink, CardFaceStyle.Ink);
        Vector2[] accent = [new(width - 26, 0), new(width - 8, 0), new(width, 8), new(width, 30), new(width - 26, 30)];
        panel.DrawColoredPolygon(accent, CardFaceStyle.Accent(card));
        CardEdgeSmoothing.Draw(panel, accent, CardFaceStyle.Accent(card));
    }
}
