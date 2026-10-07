using Godot;
using Marvel.View;


namespace Marvel.Godot;

/// <summary>Constructs full, compact, and concealed card faces.</summary>
internal static class CardFaceRendering
{
    internal static Control CreateBody(
        BoardCardPresentation card,
        CardDisplaySize size,
        CardLayoutMetrics layout,
        InterfaceScale scale,
        ICardArtProvider? art) =>
        card.Concealed
            ? Back(card, layout)
            : PrintedCardFace.Create(card, layout, scale, art, size);

    private static Control Back(BoardCardPresentation card, CardLayoutMetrics layout)
    {
        Vector2 size = new(layout.Width - 8, layout.MinimumHeight - 8);
        var content = new Control { Name = "CardBack", CustomMinimumSize = size,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        string caption = string.IsNullOrWhiteSpace(card.Back) ? "Concealed" : TabletopAreaNames.Title(card.Back);
        if (card.Count > 1) caption += $"\n{card.Count}";
        Label label = PrintedCardFace.Text(caption, "BackIdentity", new Rect2(Vector2.Zero, size), 30 * layout.Width / 400f);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AddThemeColorOverride("font_color", Colors.White);
        content.AddChild(label);
        return content;
    }
}
