using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns bounded card identity and distinct cost/stage corner treatments.</summary>
internal static class PrintedCardHeader
{
    internal static void Add(Control face, BoardCardPresentation card, CardFaceRegions r)
    {
        var masthead = PrintedCardFace.Panel("Masthead", r.Title, CardFaceStyle.Paper);
        face.AddChild(masthead);
        var row = new HBoxContainer { Name = "TitleLayout", MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 0);
        masthead.AddChild(row);
        row.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        if (card.PrintedStats.Any(value => value.Name == "Unique" && value.Value == "1"))
        {
            Label mark = PrintedCardStats.Symbol("U", "Unique",
                new Rect2(0, 0, 28 * r.Unit, r.Title.Size.Y), 22 * r.Unit);
            mark.CustomMinimumSize = new Vector2(28 * r.Unit, 0);
            mark.VerticalAlignment = VerticalAlignment.Center;
            mark.AddThemeColorOverride("font_color", CardFaceStyle.Ink);
            row.AddChild(mark);
        }
        Label title = PrintedCardFace.Text(card.Title, "Title", new Rect2(Vector2.Zero, r.Title.Size),
            (r.Full ? CardVisualTokens.FullTitleSize : CardVisualTokens.CompactTitleSize) * r.Unit);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.VerticalAlignment = VerticalAlignment.Center;
        title.MaxLinesVisible = 2;
        title.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        title.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        row.AddChild(title);
        AddPrimary(face, card, r);
        string identity = string.Join(" · ", new[] { card.Kind,
            r.Full && !card.Kind.Equals(card.Classification, StringComparison.OrdinalIgnoreCase) ? card.Classification : "",
            card.Subtitle }.Where(value => !string.IsNullOrWhiteSpace(value)));
        Label kind = PrintedCardFace.Text(identity, "Kind", r.Kind, 18 * r.Unit);
        kind.AddThemeColorOverride("font_color", Colors.White);
        face.AddChild(kind);
    }

    private static void AddPrimary(Control face, BoardCardPresentation card, CardFaceRegions r)
    {
        string? primary = card.Cost ?? card.PrintedStats.FirstOrDefault(value => value.Name == "Stage")?.Value;
        if (primary is null) return;
        bool stage = card.Cost is null;
        Rect2 bounds = new(r.Cost.Position, new Vector2(r.Cost.Size.X, (stage ? 72 : 62) * r.Unit));
        var badge = PrintedCardFace.Panel("PrimaryValue", bounds, stage ? CardFaceStyle.Ink : CardFaceStyle.Paper);
        AddPrimaryNumber(badge, primary, bounds, stage, r);
        if (stage) AddStageCaption(badge, bounds, r);
        face.AddChild(badge);
    }

    private static void AddPrimaryNumber(Control badge, string primary, Rect2 bounds, bool stage, CardFaceRegions r)
    {
        Label number = PrintedCardFace.Text(stage ? Roman(primary) : primary, "PrimaryValueValue",
            new Rect2(0, 0, bounds.Size.X, (stage ? 46 : 62) * r.Unit), (stage ? 36 : 44) * r.Unit);
        number.AutowrapMode = TextServer.AutowrapMode.Off;
        number.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        number.HorizontalAlignment = HorizontalAlignment.Center;
        number.VerticalAlignment = VerticalAlignment.Center;
        number.TooltipText = stage ? $"Stage {primary}" : $"Cost {primary}";
        if (stage) number.AddThemeColorOverride("font_color", Colors.White);
        badge.AddChild(number);
    }

    private static void AddStageCaption(Control badge, Rect2 bounds, CardFaceRegions r)
    {
        Label label = PrintedCardFace.Text("Stage", "StageCaption",
            new Rect2(0, 46 * r.Unit, bounds.Size.X, 26 * r.Unit), 16 * r.Unit);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AddThemeColorOverride("font_color", Colors.White);
        badge.AddChild(label);
    }

    private static string Roman(string value) => value switch
    {
        "1" => "I", "2" => "II", "3" => "III", "4" => "IV", _ => value,
    };
}
