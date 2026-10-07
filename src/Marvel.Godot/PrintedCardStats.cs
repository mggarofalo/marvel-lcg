using Godot;

namespace Marvel.Godot;

/// <summary>Renders the stable stat rail and its independent printed marks.</summary>
internal static class PrintedCardStats
{
    internal static void Add(Control face, IReadOnlyList<CardStatValue> stats, CardFaceRegions r)
    {
        float y = r.Stats.Position.Y;
        foreach (CardStatValue stat in stats)
        {
            float height = (r.Full ? 78 : 74) * r.Unit;
            var badge = PrintedCardFace.Panel($"Stat{stat.Name}",
                new Rect2(r.Stats.Position.X, y, r.Stats.Size.X, height), ColorFor(stat.Name));
            face.AddChild(badge);
            float valueHeight = (r.Full ? 38 : 46) * r.Unit;
            Label value = PrintedCardFace.Text(stat.Value, $"SummaryValues{stat.Name}",
                new Rect2(0, 0, r.Stats.Size.X, valueHeight), (r.Full ? 30 : 40) * r.Unit);
            value.AutowrapMode = TextServer.AutowrapMode.Off;
            value.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            value.VerticalAlignment = VerticalAlignment.Center;
            value.HorizontalAlignment = HorizontalAlignment.Center;
            value.AddThemeColorOverride("font_color", Colors.White);
            badge.AddChild(value);
            AddMarks(badge, stat, r, valueHeight);
            Label name = PrintedCardFace.Text(Name(stat.Name), $"StatName{stat.Name}",
                new Rect2(0, valueHeight, r.Stats.Size.X, 26 * r.Unit), (r.Full ? 13 : 18) * r.Unit);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            name.AddThemeColorOverride("font_color", Colors.White);
            badge.AddChild(name);
            y += height + 3 * r.Unit;
        }
    }

    private static void AddMarks(Control badge, CardStatValue stat, CardFaceRegions r, float valueHeight)
    {
        if (stat.Modified)
        {
            Rect2 bounds = r.Full ? new Rect2(0, 60 * r.Unit, badge.Size.X, 18 * r.Unit)
                : new Rect2(badge.Size.X - 18 * r.Unit, 0, 18 * r.Unit, 26 * r.Unit);
            Label changed = PrintedCardFace.Text(r.Full ? $"printed {stat.Printed}" : "•", $"PrintedBase{stat.Name}",
                bounds, (r.Full ? 12 : 18) * r.Unit);
            changed.HorizontalAlignment = HorizontalAlignment.Center;
            changed.TooltipText = $"Printed {stat.Printed}; current {stat.Value}";
            changed.AddThemeColorOverride("font_color", Colors.White);
            badge.AddChild(changed);
        }
        if (stat.PerPlayer)
        {
            Label perPlayer = Symbol("G", $"PerPlayer{stat.Name}",
                new Rect2(badge.Size.X - 18 * r.Unit, 20 * r.Unit, 18 * r.Unit, 20 * r.Unit), 18 * r.Unit);
            badge.AddChild(perPlayer);
        }
        if (stat.ConsequentialDamage > 0)
        {
            Label marks = PrintedCardFace.Text(new string('✦', stat.ConsequentialDamage),
                $"Consequential{stat.Name}", new Rect2(0, valueHeight - 16 * r.Unit, badge.Size.X, 26 * r.Unit), 16 * r.Unit);
            marks.HorizontalAlignment = HorizontalAlignment.Right;
            marks.AddThemeColorOverride("font_color", Colors.White);
            marks.TooltipText = $"{stat.ConsequentialDamage} consequential damage";
            badge.AddChild(marks);
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

    private static Color ColorFor(string name) => name.TrimEnd('+') switch
    {
        "THW" => new("236e9b"), "ATK" => new("a52f36"), "DEF" => new("367849"),
        _ => CardFaceStyle.Ink,
    };
}
