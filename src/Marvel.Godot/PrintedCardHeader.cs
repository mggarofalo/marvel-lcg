using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns left-aligned identity and distinct cost/stage corner treatments.</summary>
internal static class PrintedCardHeader
{
    internal static float MeasureTitle(BoardCardPresentation card, CardFaceRegions r) =>
        CardTypography.Title.GetMultilineStringSize(card.Title.ToUpperInvariant(), HorizontalAlignment.Left,
            r.Title.Size.X - UniqueWidth(card, r), Mathf.RoundToInt(r.TitleFontSize)).Y;

    internal static void Add(Control face, BoardCardPresentation card, CardFaceRegions r)
    {
        float unique = UniqueWidth(card, r);
        if (unique > 0)
            face.AddChild(CardGlyphRendering.Create("U", "Unique",
                new Rect2(r.Title.Position + new Vector2(0, 4 * r.Density),
                    Vector2.One * (unique - 3 * r.Density)), Colors.White));
        Label title = PrintedCardFace.Text(card.Title, "Title",
            new Rect2(r.Title.Position + new Vector2(unique, 0),
                r.Title.Size - new Vector2(unique, 0)), r.TitleFontSize);
        title.Uppercase = true;
        title.HorizontalAlignment = HorizontalAlignment.Left;
        title.VerticalAlignment = VerticalAlignment.Top;
        title.MaxLinesVisible = 2;
        title.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        title.AddThemeColorOverride("font_color", Colors.White);
        face.AddChild(title);
        AddPrimary(face, card, r);
        string identity = string.Join(" · ", new[] { card.Kind, card.Subtitle }
            .Where(value => !string.IsNullOrWhiteSpace(value))).ToUpperInvariant();
        Label kind = PrintedCardFace.Text(identity, "Kind", r.Kind, (r.Full ? 12 : 8) * r.Density);
        kind.AddThemeFontOverride("font", CardTypography.Bold);
        kind.AddThemeColorOverride("font_color", Colors.White);
        face.AddChild(kind);
    }

    private static float UniqueWidth(BoardCardPresentation card, CardFaceRegions r) =>
        card.PrintedStats.Any(value => value.Name == "Unique" && value.Value == "1")
            ? (r.Full ? 19 : 11) * r.Density : 0;

    private static void AddPrimary(Control face, BoardCardPresentation card, CardFaceRegions r)
    {
        string? primary = card.Cost ?? card.PrintedStats.FirstOrDefault(value => value.Name == "Stage")?.Value;
        if (primary is null) return;
        bool stage = card.Cost is null;
        var badge = new Control { Name = "PrimaryValue", Position = r.Cost.Position,
            Size = r.Cost.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        float edge = r.Cost.Size.X;
        float cut = (r.Full ? 8 : 5) * r.Density;
        badge.AddChild(new Polygon2D { Name = "PrimaryField", Color = CardFaceStyle.Accent(card),
            Polygon = [Vector2.Zero, new(edge, 0), new(edge, edge - cut),
                new(edge - cut, edge), new(0, edge)] });
        Label number = PrintedCardFace.Text(stage ? Roman(primary) : primary, stage ? "StageValue" : "PrimaryValueValue",
            new Rect2(Vector2.Zero, r.Cost.Size), (r.Full ? 36 : 22) * r.Density);
        number.AutowrapMode = TextServer.AutowrapMode.Off;
        number.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        number.HorizontalAlignment = HorizontalAlignment.Center;
        number.VerticalAlignment = VerticalAlignment.Center;
        number.AddThemeColorOverride("font_color", Colors.White);
        number.TooltipText = stage ? $"Stage {primary}" : $"Cost {primary}";
        badge.AddChild(number);
        face.AddChild(badge);
    }

    private static string Roman(string value) => value switch
    {
        "1" => "I", "2" => "II", "3" => "III", "4" => "IV", _ => value,
    };
}
