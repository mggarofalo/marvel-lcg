using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders printed scheme icons independently from live state.</summary>
internal static class CardPrintedIcons
{
    internal static int RowCount(BoardCardPresentation card) => (Entries(card).Count + 2) / 3;

    internal static void Add(Control face, BoardCardPresentation card, CardFaceRegions r)
    {
        var entries = Entries(card);
        float cellWidth = r.Tokens.Size.X / 3;
        float cellHeight = (r.Full ? 32 : 44) * r.Unit;
        for (int index = 0; index < entries.Count; index++)
        {
            (string name, string text, string? glyph) = entries[index];
            var token = PrintedCardFace.Panel($"Token{name}", new Rect2(
                r.Tokens.Position + new Vector2(index % 3 * cellWidth, index / 3 * cellHeight),
                new Vector2(cellWidth - 2 * r.Unit, cellHeight - 2 * r.Unit)), CardFaceStyle.Ink);
            face.AddChild(token);
            var contents = new MarginContainer
            {
                Name = "TokenContents", MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            token.AddChild(contents);
            contents.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            Label label = PrintedCardFace.Text(text, $"Status{name}",
                new Rect2(Vector2.Zero, token.Size), (r.Full ? 16 : 24) * r.Unit);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            label.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeColorOverride("font_color", Colors.White);
            label.TooltipText = name;
            contents.AddChild(label);
            if (glyph is not null)
            {
                label.Text = text == "1" ? "" : text;
                label.HorizontalAlignment = HorizontalAlignment.Right;
                Label icon = PrintedCardStats.Symbol(glyph, $"Icon{name}",
                    new Rect2(2 * r.Unit, 0, token.Size.Y, token.Size.Y), (r.Full ? 24 : 34) * r.Unit);
                icon.VerticalAlignment = VerticalAlignment.Center;
                token.AddChild(icon);
            }
        }
    }

    private static List<(string Name, string Text, string? Glyph)> Entries(BoardCardPresentation card)
    {
        var result = new List<(string Name, string Text, string? Glyph)>();
        foreach (string name in new[] { "Acceleration", "Amplify", "Crisis", "Hazard" })
        {
            string? value = card.PrintedStats.FirstOrDefault(field => field.Name == name)?.Value;
            if (value is null or "0") continue;
            CardSymbols.TryGet(name, out string? glyph);
            result.Add((name, value, glyph));
        }
        return result;
    }
}
