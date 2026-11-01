using Marvel.View;

using static Marvel.Client.ClientGameAreaValidation;

namespace Marvel.Client;

/// <summary>Checks Kang's progress and departure facts name only what the response lists.</summary>
internal static class ClientKangValidation
{
    internal static bool Valid(WorldDescriptor world, KangTableDescriptor kang, Known known)
    {
        if (kang.Stage is < 1 or > 3 || kang.RemainingLocalKangs < 0 || kang.StoredDominions < 0) return false;
        if (kang.StartingPlayers != world.Players.Count || !ValidReferences(kang, known)) return false;
        if (!Zone(world, kang.EncounterDeck, "EncounterDeck") || !Zone(world, kang.EncounterDiscard, "EncounterDiscardPile"))
            return false;
        return kang.Progress is not null && kang.Notes is not null && kang.Notes.All(note => note is not null);
    }

    private static bool ValidReferences(KangTableDescriptor kang, Known known)
    {
        if (!Distinct(kang.CentralSchemes, known.Cards)) return false;
        if (kang.PendingSplit is { } split && !known.Cards.Contains(split)) return false;
        if (!ValidDepartures(kang.Departures, known)) return false;
        return kang.Acting is not { } acting || ValidActing(acting, known);
    }

    private static bool ValidDepartures(IReadOnlyList<KangDepartureDescriptor>? departures, Known known)
    {
        if (departures is null || !departures.All(item => item is not null && ValidDeparture(item, known))) return false;
        return departures.Select(item => item.Villain).Distinct().Count() == departures.Count;
    }

    private static bool ValidDeparture(KangDepartureDescriptor departure, Known known)
    {
        if (departure.Summary is null || !known.Cards.Contains(departure.Villain)) return false;
        if (!known.Areas.Contains(departure.SourceGameArea) || !Distinct(departure.Seats, known.Seats)) return false;
        if (departure.CompletedScheme is { } scheme && !known.Cards.Contains(scheme)) return false;
        if (!Distinct(departure.Destinations, known.Areas)) return false;
        return !departure.Destinations.Contains(departure.SourceGameArea)
            && departure.Reunion == (departure.Destinations.Count == 0);
    }

    private static bool ValidActing(VillainPhaseFocusDescriptor acting, Known known)
    {
        if (acting.Step is not (1 or 2 or 4) || acting.Summary is null) return false;
        return known.Seats.Contains(acting.Seat) && (acting.GameArea is not { } area || known.Areas.Contains(area));
    }

    private static bool Zone(WorldDescriptor world, int id, string zone) =>
        world.Areas.Count(area => area.Id == id && area.Owner == -1 && area.Zone == zone) == 1;
}
