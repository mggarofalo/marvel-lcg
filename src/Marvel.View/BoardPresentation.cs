using System.Globalization;
using System.Text;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>A display-only board derived from one visibility-safe snapshot.</summary>
public sealed record BoardPresentation(IReadOnlyList<BoardAreaPresentation> Areas)
{
    private static readonly HashSet<string> LiveZeroFields = new(
        ["attack", "defense", "recover", "scheme", "thwart"],
        StringComparer.Ordinal);

    /// <summary>Scenario, player, and fallback lanes used by the tabletop renderer.</summary>
    public IReadOnlyList<BoardLanePresentation> Lanes { get; init; } = [];

    /// <summary>Visibility-safe compact seat facts for the tabletop seat strip.</summary>
    public IReadOnlyList<PlayerSummaryDescriptor> PlayerSummaries { get; init; } = [];

    /// <summary>Host-selected table roles; this never contains private card detail.</summary>
    public TableContextDescriptor? Table { get; init; }

    /// <summary>Builds a fresh presentation without retaining or enriching engine state.</summary>
    public static BoardPresentation From(WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var players = world.Players.ToDictionary(player => player.Seat);
        var asideOrdinals = world.Areas
            .Where(area => area.Zone == nameof(DeckType.AsideDeck))
            .GroupBy(area => area.Owner)
            .SelectMany(group => group.Select((area, ordinal) => (area.Id, ordinal)))
            .ToDictionary(pair => pair.Id, pair => pair.ordinal);
        BoardAreaPresentation[] areas = CombineProgressiveAreas(
            [.. world.Areas.Select(area => Present(
                area,
                players,
                asideOrdinals.GetValueOrDefault(area.Id, -1)))]);
        BoardPlayerPresentation[] seats =
            [.. world.Players.Select(player => new BoardPlayerPresentation(
                player.Seat, player.Name))];
        return new BoardPresentation(areas)
        {
            Lanes = BoardLayout.Arrange(areas, seats),
            PlayerSummaries = world.PlayerSummaries,
            Table = world.Table,
        };
    }

    private static BoardAreaPresentation[] CombineProgressiveAreas(
        BoardAreaPresentation[] source)
    {
        var hidden = new HashSet<int>();
        var result = new List<BoardAreaPresentation>(source.Length);
        foreach (BoardAreaPresentation area in source)
        {
            if (hidden.Contains(area.Id))
            {
                continue;
            }
            if (ShouldHideProgressiveDeck(area, source))
            {
                continue;
            }

            BoardAreaPresentation? upcoming = UpcomingArea(area, source);
            if (upcoming is null)
            {
                result.Add(area);
                continue;
            }

            hidden.Add(upcoming.Id);
            BoardCardPresentation[] cards = [.. area.Cards, .. upcoming.Cards];
            BoardCardPresentation[] removed = [.. area.Removed, .. upcoming.Removed];
            result.Add(area with
            {
                Title = area.Zone == "VillainArea" ? "VILLAIN" : "MAIN SCHEME",
                Context = "Current and upcoming stages",
                Cards = cards,
                Removed = removed,
                Prominence = Prominence(area.Zone, cards.Length + removed.Length),
            });
        }

        return [.. result];
    }

    private static bool ShouldHideProgressiveDeck(
        BoardAreaPresentation area, BoardAreaPresentation[] source)
    {
        string? currentZone = area.Zone switch
        {
            "VillainDeck" => "VillainArea",
            "MainSchemesDeck" => "MainSchemesArea",
            _ => null,
        };
        return currentZone is not null && source.Any(candidate =>
            candidate.Seat == area.Seat
            && candidate.Host == area.Host
            && candidate.Zone == currentZone);
    }

    private static BoardAreaPresentation? UpcomingArea(
        BoardAreaPresentation area, BoardAreaPresentation[] source)
    {
        string? deckZone = area.Zone switch
        {
            "VillainArea" => "VillainDeck",
            "MainSchemesArea" => "MainSchemesDeck",
            _ => null,
        };
        return deckZone is null ? null : source.FirstOrDefault(candidate =>
            candidate.Zone == deckZone && candidate.Seat == area.Seat && candidate.Host == area.Host);
    }

    private static BoardAreaPresentation Present(
        AreaDescriptor area,
        Dictionary<int, PlayerDescriptor> players,
        int asideOrdinal)
    {
        string owner = players.TryGetValue(area.Owner, out PlayerDescriptor? player)
            ? player.Name
            : area.Owner < 0 ? "Scenario" : $"Seat {area.Owner}";
        // These labels are a client choice. The tabletop rules define the zone,
        // not the diagnostic wording that distinguishes it from set-aside cards.
        string title = area.Zone switch
        {
            "VillainDeck" => "UPCOMING VILLAIN STAGES",
            "AsideDeck" when asideOrdinal == 1 => $"{owner.ToUpperInvariant()}'S NEMESIS SET",
            "AsideDeck" => $"{owner.ToUpperInvariant()}'S SET-ASIDE AREA",
            _ => BoardCardPresentationFactory.Humanize(area.Zone, trimArea: true).ToUpperInvariant(),
        };
        string context = area.Zone == "VillainDeck"
            ? "Out of play · enters after the current stage"
            : area.Owner < 0 ? "Scenario" : owner;
        if (area.Host >= 0)
        {
            context += " · hosted area";
        }

        return new BoardAreaPresentation(
            area.Id,
            title,
            context,
            BoardCardPresentationFactory.Present(area.Cards, area.Zone),
            BoardCardPresentationFactory.Present(area.Removed, area.Zone))
        {
            Zone = area.Zone,
            Seat = area.Owner,
            Host = area.Host,
            Prominence = Prominence(area.Zone, area.Cards.Count + area.Removed.Count),
        };
    }

    private static BoardAreaProminence Prominence(string zone, int cardCount)
    {
        if (cardCount == 0)
        {
            return BoardAreaProminence.Empty;
        }

        return BoardCardPresentationFactory.IsInPlay(zone) || zone == "StatusArea"
            ? BoardAreaProminence.Live
            : BoardAreaProminence.Supporting;
    }

}
