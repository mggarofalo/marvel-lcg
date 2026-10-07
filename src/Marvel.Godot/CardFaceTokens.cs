using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Places resources and live tokens at stable locations on a printed face.</summary>
internal static class CardFaceTokens
{
    internal static void Add(Control face, BoardCardPresentation card, CardFaceRegions regions)
    {
        AddResources(face, card, regions);
        AddProgress(face, card, regions);
        CardStatusTokens.Add(face, card, regions);
        CardRetaliateToken.Add(face, card, regions);
    }

    private static void AddResources(Control face, BoardCardPresentation card, CardFaceRegions r)
    {
        var icons = new HBoxContainer { Name = "ResourceIcons", Position = r.Resources.Position,
            Size = r.Resources.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        face.AddChild(icons);
        string resources = card.PrintedStats.FirstOrDefault(value => value.Name == "RES")?.Value ?? "";
        int index = 0;
        foreach ((string name, string glyph) in CardRulesMarkup.ResourceTokens(resources))
        {
            Label slot = PrintedCardFace.Text(glyph, $"InspectorResourceIconSlot{index++}",
                new Rect2(0, 0, CardVisualTokens.ResourceSlot * r.Unit, CardVisualTokens.ResourceSlot * r.Unit), CardVisualTokens.ResourceFontSize * r.Unit);
            slot.CustomMinimumSize = new Vector2(CardVisualTokens.ResourceSlot, CardVisualTokens.ResourceSlot) * r.Unit;
            slot.HorizontalAlignment = HorizontalAlignment.Center;
            slot.VerticalAlignment = VerticalAlignment.Center;
            slot.AutowrapMode = TextServer.AutowrapMode.Off;
            slot.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            slot.TooltipText = name;
            ResourceIconRendering.Style(slot, glyph[0]);
            icons.AddChild(slot);
        }
        if (index == 0 && card.PrintedStats.FirstOrDefault(value => value.Name == "Boost") is { } boost)
        {
            Label label = PrintedCardFace.Text(new string('B', int.TryParse(boost.Value, out int count) ? count : 0),
                "BoostIcons", r.Resources, 26 * r.Unit);
            label.TooltipText = $"Boost {boost.Value}";
            label.AddThemeFontOverride("font", CardRulesMarkup.ResourceFont());
            label.AddThemeColorOverride("font_color", Colors.White);
            face.AddChild(label);
        }
    }

    private static void AddProgress(Control face, BoardCardPresentation card, CardFaceRegions r)
    {
        CardProgressValue? value = CardProgressValue.From(card);
        if (value is null) return;
        bool threat = value.IsThreat;
        var token = PrintedCardFace.Panel("ProgressToken", r.Health,
            threat ? new Color("d4a725") : new Color("9d242b"));
        face.AddChild(token);
        Label symbol = PrintedCardFace.Text(threat ? "◆" : "♥", "ProgressSymbol",
            new Rect2(0, 0, 24 * r.Unit, r.Health.Size.Y), 23 * r.Unit);
        symbol.AutowrapMode = TextServer.AutowrapMode.Off;
        symbol.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        symbol.ClipText = false;
        symbol.VerticalAlignment = VerticalAlignment.Center;
        symbol.AddThemeColorOverride("font_color", threat ? CardFaceStyle.Ink : Colors.White);
        token.AddChild(symbol);
        Label label = PrintedCardFace.Text(value.Value, $"ProgressValues{value.FieldName}",
            new Rect2(24 * r.Unit, 0, r.Health.Size.X - 24 * r.Unit, r.Health.Size.Y), 35 * r.Unit);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AddThemeColorOverride("font_color", threat ? CardFaceStyle.Ink : Colors.White);
        label.TooltipText = threat ? $"Threat {value.Value}" : $"Hit points {value.Value}";
        token.AddChild(label);
        if (value.PerPlayer)
        {
            Label perPlayer = PrintedCardStats.Symbol("G", "ProgressPerPlayer",
                new Rect2(r.Health.Size.X - 18 * r.Unit, 0, 18 * r.Unit, 18 * r.Unit), 18 * r.Unit);
            perPlayer.AddThemeColorOverride("font_color", threat ? CardFaceStyle.Ink : Colors.White);
            token.AddChild(perPlayer);
        }
    }

}
