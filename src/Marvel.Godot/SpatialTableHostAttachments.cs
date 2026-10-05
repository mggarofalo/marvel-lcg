using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Offers bounded attachment inspection beside the host's readable face.</summary>
internal static class SpatialTableHostAttachments
{
    internal static void Add(
        CardControl host, BoardAreaPresentation area, BoardRenderResult result,
        InterfaceScale scale, ICardArtProvider? art)
    {
        BoardCardPresentation[] cards = SpatialTableZones.Current(area);
        if (cards.Length == 0) return;
        var inspect = new Button
        {
            Name = $"InspectAttachments{area.Host}",
            Text = Caption(area, cards),
            TooltipText = $"Attachments on {area.HostedBy}.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 44),
        };
        inspect.Pressed += () =>
        {
            if (result.IsCurrent?.Invoke() == true)
                TabletopPileInspector.Show(inspect, TabletopAreaObject.From(area), result, scale, art);
        };
        SpatialCardSidecar.For(host).AddChild(inspect);
        TableCompactButtonStyle.Apply(inspect);
    }

    internal static string Caption(BoardAreaPresentation area, BoardCardPresentation[] cards) =>
        cards.All(card => card.Concealed)
            ? $"{TabletopAreaNames.Region(area.Zone)} · {cards.Sum(card => card.Count)}\nFace down"
            : cards.Length == 1 ? cards[0].Title : $"Attached\n{cards.Length}";
}
