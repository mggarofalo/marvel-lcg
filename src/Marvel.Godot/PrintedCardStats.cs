using Godot;

namespace Marvel.Godot;

/// <summary>Renders a horizontal stat rail with independent marks around shaped numerals.</summary>
internal static class PrintedCardStats
{
    internal static void Add(Control face, IReadOnlyList<CardStatValue> stats, CardFaceRegions r)
    {
        if (stats.Count == 0) return;
        float width = r.Stats.Size.X / stats.Count;
        for (int index = 0; index < stats.Count; index++)
        {
            CardStatValue stat = stats[index];
            var cell = new Control { Name = $"Stat{stat.Name}",
                Position = r.Stats.Position + new Vector2(width * index, 0),
                Size = new Vector2(width, r.Stats.Size.Y), MouseFilter = Control.MouseFilterEnum.Ignore };
            face.AddChild(cell);
            AddValue(cell, stat, r);
        }
    }

    private static void AddValue(Control cell, CardStatValue stat, CardFaceRegions r)
    {
        int fontSize = Math.Max(5, Mathf.RoundToInt((r.Full ? CardVisualTokens.FullStatSize : CardVisualTokens.CompactStatSize) * r.Density));
        float icon = (r.Full ? 22 : 11) * r.Density;
        float gap = (r.Full ? 7 : 4) * r.Density;
        using var line = new TextLine();
        line.AddString(stat.IsBareStar ? "0" : stat.Value, CardTypography.Bold, fontSize);
        Vector2 measured = line.GetSize();
        Rect2 number = new(icon + gap, 0, measured.X, measured.Y);
        AddNumber(cell, stat, number, fontSize);
        AddIdentity(cell, stat, r, measured, line.GetLineAscent());
        cell.TooltipText = $"{Name(stat.Name)} {stat.Value}";
        var geometry = new CardNumberGeometry(number, line.GetLineAscent(),
            new CardNumberMarks(stat.SpecialStar && !stat.IsBareStar, stat.PerPlayer, stat.ConsequentialDamage),
            r.Density, r.Full);
        AddMarks(cell, stat, geometry);
    }

    private static void AddNumber(Control cell, CardStatValue stat, Rect2 number, int fontSize)
    {
        if (stat.IsBareStar)
        {
            cell.AddChild(CardGlyphRendering.Create("S", $"SummaryValues{stat.Name}",
                new Rect2(number.Position, Vector2.One * fontSize), Colors.White));
            return;
        }
        Label value = PrintedCardFace.Text(stat.Value, $"SummaryValues{stat.Name}", number, fontSize);
        value.AddThemeFontOverride("font", CardTypography.Bold);
        value.AutowrapMode = TextServer.AutowrapMode.Off;
        value.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        value.AddThemeColorOverride("font_color", Colors.White);
        cell.AddChild(value);
    }

    private static void AddIdentity(Control cell, CardStatValue stat, CardFaceRegions r, Vector2 measured, float baseline)
    {
        float size = (r.Full ? 22 : 11) * r.Density;
        Rect2 bounds = new(0, (measured.Y - size) / 2, size, size);
        string? glyph = stat.Name == "REC" ? null : CardGlyphRendering.Stat(stat.Name);
        if (glyph is not null)
            cell.AddChild(CardGlyphRendering.Create(glyph, $"StatIcon{stat.Name}", bounds, Colors.White));
        else
            AddCaption(cell, Name(stat.Name), $"StatIcon{stat.Name}", bounds, (r.Full ? 10 : 5.5f) * r.Density);
        if (r.Full)
            AddCaption(cell, Name(stat.Name), $"StatName{stat.Name}",
                new Rect2(29 * r.Density, baseline + 3 * r.Density,
                    cell.Size.X - 29 * r.Density,
                    CardTypography.Title.GetHeight(Math.Max(5, Mathf.RoundToInt(10 * r.Density)))), 10 * r.Density);
    }

    private static void AddCaption(Control cell, string text, string name, Rect2 bounds, float fontSize)
    {
        Label label = PrintedCardFace.Text(text, name, bounds, fontSize);
        label.AddThemeColorOverride("font_color", Colors.White);
        cell.AddChild(label);
    }

    private static void AddMarks(Control cell, CardStatValue stat, CardNumberGeometry geometry)
    {
        if (stat.Modified)
        {
            var underline = new ColorRect { Name = $"Modified{stat.Name}",
                Position = geometry.Underline.Position, Size = geometry.Underline.Size,
                Color = ClientTheme.ToGodot(CardVisualTokens.Modified), MouseFilter = Control.MouseFilterEnum.Ignore };
            cell.AddChild(underline);
        }
        if (geometry.Special.HasArea())
            cell.AddChild(CardGlyphRendering.Create("S", $"SpecialStar{stat.Name}", geometry.Special, Colors.White));
        if (geometry.PerPlayer.HasArea())
            cell.AddChild(Symbol("G", $"PerPlayer{stat.Name}", geometry.PerPlayer, geometry.PerPlayer.Size.Y));
        for (int index = 0; index < geometry.Consequences.Count; index++)
        {
            TextureRect mark = CardGlyphRendering.Create("D", $"Consequential{stat.Name}{index}",
                geometry.Consequences[index], Colors.White);
            mark.TooltipText = $"{stat.ConsequentialDamage} consequential damage";
            cell.AddChild(mark);
        }
    }

    internal static Label Symbol(string glyph, string name, Rect2 bounds, float size)
    {
        Label label = PrintedCardFace.Text(glyph, name, bounds, size);
        label.AddThemeFontOverride("font", CardRulesMarkup.ResourceFont());
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        label.ClipText = false;
        label.Size = bounds.Size;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        return label;
    }

    private static string Name(string name) => name switch
    {
        "StartingThreat" => "Start", "EscalationThreat" => "+Threat", "HS" => "Hand", _ => name.TrimEnd('+'),
    };
}
