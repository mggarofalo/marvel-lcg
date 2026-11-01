using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Host-local continuous attachments and visibility-safe facedown counts.</summary>
internal static class SpatialTableHostAttachments
{
    internal static void AddOther(CardControl host, BoardAreaPresentation area,
        BoardRenderResult result, InterfaceScale scale, ICardArtProvider? art)
    {
        BoardCardPresentation[] cards = [.. area.Cards.Where(card => !CardSourceGroups.IsLocalSource(card))];
        if (cards.Length == 0) return;
        if (cards.All(card => card.Concealed))
        {
            SpatialCardSidecar.Caption(host, Caption(area, cards));
            return;
        }
        SpatialCardSidecar.For(host).AddChild(CardSourceCollection.Create(cards, host.Size.X, result, scale,
            TabletopAreaNames.Region(area.Zone)));
    }

    /// <summary>The caption and card rows <see cref="Add"/> and <see cref="AddOther"/> hang below a host.</summary>
    internal static int Rows(IReadOnlyList<BoardAreaPresentation> areas, int host)
    {
        int attached = CardSourceGroups.Attached(areas, host).Length;
        int rows = attached == 0 ? 0 : 1 + attached;
        foreach (BoardAreaPresentation area in areas.Where(area => area.Host == host))
        {
            BoardCardPresentation[] cards = [.. area.Cards.Where(card => !CardSourceGroups.IsLocalSource(card))];
            if (cards.Length > 0) rows += cards.All(card => card.Concealed) ? 1 : 1 + cards.Length;
        }
        return rows;
    }

    internal static string Caption(BoardAreaPresentation area, BoardCardPresentation[] cards)
    {
        int count = cards.Sum(card => card.Count);
        return cards.All(card => card.Concealed)
            ? area.Zone is "BoostingArea" or "BoostCardsDeck" ? $"{count} facedown boost {(count == 1 ? "card" : "cards")}"
                : $"{TabletopAreaNames.Region(area.Zone)} · {count} facedown {(count == 1 ? "card" : "cards")}"
            : cards.Length == 1 ? cards[0].Title : $"{TabletopAreaNames.Region(area.Zone)} · {cards.Length}";
    }

    internal static void Add(CardControl host, BoardCardPresentation[] cards,
        BoardRenderResult result, InterfaceScale scale, ICardArtProvider? art = null)
    {
        if (cards.Length == 0) return;
        SpatialCardSidecar.For(host).AddChild(CardSourceCollection.Create(cards, host.Size.X, result, scale, "Attached"));
    }
}
