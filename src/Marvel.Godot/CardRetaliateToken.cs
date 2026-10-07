using System.Globalization;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Shows the supplied live retaliation value separately from printed rules.</summary>
internal static class CardRetaliateToken
{
    internal static string Caption(BoardCardPresentation card) =>
        !card.Concealed && card.Retaliate is > 0
            ? $"Retaliate {card.Retaliate.Value.ToString(CultureInfo.InvariantCulture)}" : string.Empty;

    internal static void Add(Control face, BoardCardPresentation card, CardFaceRegions regions)
    {
        string caption = Caption(card);
        if (caption.Length == 0) return;
        var token = PrintedCardFace.Panel("LiveRetaliate", regions.Retaliate, CardFaceStyle.Ink);
        face.AddChild(token);
        Label label = PrintedCardFace.Text(caption, "LiveRetaliateValue",
            new Rect2(Vector2.Zero, token.Size), (regions.Full ? 20 : 28) * regions.Unit);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AddThemeColorOverride("font_color", Colors.White);
        label.TooltipText = "Current Retaliate value";
        token.AddChild(label);
    }
}
