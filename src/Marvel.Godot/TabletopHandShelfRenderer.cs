using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders the expanded seat's pinned hand without treating concealed cards as empty.</summary>
internal static class TabletopHandShelfRenderer
{
    internal static void Render(
        BoardPresentation board,
        int seat,
        HBoxContainer destination,
        Label heading,
        BoardRenderResult result,
        InterfaceScale scale,
        ICardArtProvider? art)
    {
        BoardAreaPresentation? hand = board.Areas.FirstOrDefault(area =>
            area.Zone == "HandsArea" && area.Seat == seat);
        BoardCardPresentation[] visible = hand?.Cards.Where(card => !card.Concealed).ToArray() ?? [];
        int concealed = hand?.Cards.Where(card => card.Concealed).Sum(card => card.Count) ?? 0;
        TabletopHandShelfSummary summary = TabletopHandShelfSummary.From(visible, concealed);
        heading.Text = summary.Heading;
        if (visible.Length == 0)
        {
            destination.AddChild(Label(summary.EmptyMessage));
            return;
        }

        foreach (BoardCardPresentation card in visible)
        {
            CardControl control = CardControl.Create(card, CardDisplaySize.Hand, scale, art);
            destination.AddChild(control);
            if (card.TargetId is { } target)
            {
                result.Register(target, control);
            }
            result.TrackCard(control, card, isHandCard: true);
        }
    }

    private static Label Label(string text) => new()
    {
        Text = text,
        ThemeTypeVariation = GodotThemeVariations.MutedText,
    };
}
