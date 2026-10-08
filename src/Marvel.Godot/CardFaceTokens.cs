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
    }

    private static void AddResources(Control face, BoardCardPresentation card, CardFaceRegions r)
    {
        var icons = new HBoxContainer { Name = "ResourceIcons", Position = r.Resources.Position,
            Size = r.Resources.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        icons.AddThemeConstantOverride("separation", Math.Max(1, Mathf.RoundToInt(2 * r.Density)));
        face.AddChild(icons);
        float slotSize = (r.Full ? 36 : 18) * r.Density;
        float fontSize = (r.Full ? 30 : 17) * r.Density;
        string resources = card.PrintedStats.FirstOrDefault(value => value.Name == "RES")?.Value ?? "";
        int index = 0;
        foreach ((string name, string glyph) in CardRulesMarkup.ResourceTokens(resources))
        {
            Label slot = PrintedCardFace.Text(glyph, $"InspectorResourceIconSlot{index++}",
                new Rect2(0, 0, slotSize, slotSize), fontSize);
            slot.CustomMinimumSize = Vector2.One * slotSize;
            slot.HorizontalAlignment = HorizontalAlignment.Center;
            slot.VerticalAlignment = VerticalAlignment.Center;
            slot.AutowrapMode = TextServer.AutowrapMode.Off;
            slot.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            slot.TooltipText = name;
            ResourceIconRendering.Style(slot, glyph[0]);
            icons.AddChild(slot);
        }
        if (index == 0 && card.PrintedMarks.FirstOrDefault(value => value.Attribute == "Boost") is { } boost)
        {
            int count = int.TryParse(boost.Value, out int number) ? number : 0;
            for (int mark = 0; mark < count; mark++)
                face.AddChild(CardGlyphRendering.Create("boost", $"BoostIcon{mark}",
                    new Rect2(r.Resources.Position + new Vector2(mark * slotSize, 0), Vector2.One * slotSize), CardFaceStyle.Ink));
            if (boost.SpecialStar)
                face.AddChild(CardGlyphRendering.Create("S", "BoostSpecialStar",
                    new Rect2(r.Resources.Position + new Vector2(count * slotSize, 0), Vector2.One * slotSize), CardFaceStyle.Ink));
        }
    }

    private static void AddProgress(Control face, BoardCardPresentation card, CardFaceRegions r)
    {
        CardProgressValue? value = CardProgressValue.From(card);
        if (value is null) return;
        bool threat = value.IsThreat;
        var token = PrintedCardFace.Panel("ProgressToken", r.Health,
            threat ? new Color("d4a725") : CardFaceStyle.Ink);
        face.AddChild(token);
        float iconSize = (r.Full ? 19 : 11) * r.Density;
        token.AddChild(CardGlyphRendering.Create(threat ? "SCH" : "heart", "ProgressSymbol",
            new Rect2(5 * r.Density, (r.Health.Size.Y - iconSize) / 2, iconSize, iconSize),
            threat ? CardFaceStyle.Ink : Colors.White));
        AddProgressNumber(token, value, r);
    }

    private static void AddProgressNumber(Control token, CardProgressValue value, CardFaceRegions r)
    {
        bool threat = value.IsThreat;
        Label label = PrintedCardFace.Text(value.Value, $"ProgressValues{value.FieldName}",
            new Rect2(24 * r.Unit, 0, r.Health.Size.X - 24 * r.Unit, r.Health.Size.Y), 35 * r.Unit);
        label.AddThemeFontOverride("font", CardTypography.Bold);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AddThemeColorOverride("font_color", threat ? CardFaceStyle.Ink : Colors.White);
        label.TooltipText = threat ? $"Threat {value.Value}" : $"Hit points {value.Value}";
        token.AddChild(label);
        AddMaximumMark(token, label, value, r);
        if (value.PerPlayer)
        {
            Label perPlayer = PrintedCardStats.Symbol("G", "ProgressPerPlayer",
                new Rect2(r.Health.Size.X - 18 * r.Unit, 0, 18 * r.Unit, 18 * r.Unit), 18 * r.Unit);
            perPlayer.AddThemeColorOverride("font_color", threat ? CardFaceStyle.Ink : Colors.White);
            token.AddChild(perPlayer);
        }
    }

    private static void AddMaximumMark(Control token, Label label, CardProgressValue value, CardFaceRegions r)
    {
        if (!value.MaximumModified) return;
        int slash = value.Value.IndexOf('/');
        if (slash < 0) return;
        int size = label.GetThemeFontSize("font_size");
        float whole = CardTypography.Bold.GetStringSize(value.Value, fontSize: size).X;
        float maximum = CardTypography.Bold.GetStringSize(value.Value[(slash + 1)..], fontSize: size).X;
        var line = new ColorRect { Name = "ModifiedHealthMaximum",
            Position = new Vector2(label.Position.X + (label.Size.X + whole) / 2 - maximum, token.Size.Y - 3 * r.Density),
            Size = new Vector2(maximum, Math.Max(1, 1.5f * r.Density)),
            Color = ClientTheme.ToGodot(CardVisualTokens.Modified), MouseFilter = Control.MouseFilterEnum.Ignore };
        token.AddChild(line);
    }

}
