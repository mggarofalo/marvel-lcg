using Marvel.View;

namespace Marvel.Godot;

/// <summary>Chooses the fixed-footprint tabletop object used for one projected area.</summary>
internal sealed record TabletopAreaObject(
    BoardAreaPresentation Area,
    bool IsPile,
    int Count,
    IReadOnlyList<BoardCardPresentation> InspectionOrder)
{
    private static readonly HashSet<string> PileZones = new(
        ["DiscardPile", "EncounterDiscardPile", "AsideDeck"],
        StringComparer.Ordinal);

    internal static TabletopAreaObject From(BoardAreaPresentation area)
    {
        ArgumentNullException.ThrowIfNull(area);
        BoardCardPresentation[] cards = [.. area.Cards.Concat(area.Removed)];
        return new TabletopAreaObject(
            area,
            PileZones.Contains(area.Zone),
            cards.Sum(card => card.Count),
            [.. cards.Where(card => !card.Concealed).Reverse()]);
    }

    internal BoardCardPresentation? Top =>
        InspectionOrder.Count == 0 ? null : InspectionOrder[0];

    internal string Detail
    {
        get
        {
            if (Top is { } top && Area.Zone != "AsideDeck")
            {
                return $"Top · {top.Title}";
            }
            BoardCardPresentation? concealed = Area.Cards.Concat(Area.Removed)
                .FirstOrDefault(card => card.Concealed);
            return concealed is not null && !string.IsNullOrWhiteSpace(concealed.Back)
                ? $"{concealed.Back} back · order hidden"
                : "Contents tucked away";
        }
    }
}
