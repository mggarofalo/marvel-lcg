using Marvel.Rules.Play;
using Marvel.View;

using static Marvel.Client.ClientGameAreaValidation;

namespace Marvel.Client;

/// <summary>Checks Kang's progress and departure facts name only what the response lists.</summary>
internal static class ClientKangValidation
{
    internal static bool Valid(WorldDescriptor world, KangTableDescriptor kang, Known known)
    {
        if (kang.Stage is < 1 or > 3 || kang.RemainingLocalKangs < 0 || kang.StoredDominions < 0) return false;
        // The split deals one Kang (II) per participating player, and none enters play any other way.
        if (kang.LocalKangsDealt < 0 || kang.LocalKangsDealt > world.Players.Count
            || kang.RemainingLocalKangs > kang.LocalKangsDealt) return false;
        if (kang.StartingPlayers != world.Players.Count || !ValidReferences(world, kang, known)) return false;
        return Zone(world, kang.EncounterDeck, "EncounterDeck") && Zone(world, kang.EncounterDiscard, "EncounterDiscardPile");
    }

    private static bool ValidReferences(WorldDescriptor world, KangTableDescriptor kang, Known known)
    {
        if (!Distinct(kang.CentralSchemes, known.Cards.Keys)) return false;
        if (kang.PendingSplit is { } split && !known.Cards.ContainsKey(split)) return false;
        if (!ValidDepartures(kang.Departures, known) || !ValidNotes(kang.Notes, known)) return false;
        return kang.Acting is not { } acting || ValidActing(world, acting, known);
    }

    private static bool ValidDepartures(IReadOnlyList<KangDepartureDescriptor>? departures, Known known)
    {
        if (departures is null || !departures.All(item => item is not null && ValidDeparture(item, known))) return false;
        return departures.Select(item => item.Villain).Distinct().Count() == departures.Count;
    }

    // A departing Kang still in play names the area he leaves; one already removed names none.
    private static bool ValidDeparture(KangDepartureDescriptor departure, Known known)
    {
        if (!known.Cards.TryGetValue(departure.Villain, out int? located)) return false;
        if (located is { } area && area != departure.SourceGameArea) return false;
        if (!known.Areas.Contains(departure.SourceGameArea) || !Distinct(departure.Seats, known.Seats)) return false;
        if (departure.CompletedScheme is { } scheme && !known.Cards.ContainsKey(scheme)) return false;
        return ValidDestinations(departure, known);
    }

    private static bool ValidDestinations(KangDepartureDescriptor departure, Known known) =>
        Distinct(departure.Destinations, known.Areas) && !departure.Destinations.Contains(departure.SourceGameArea)
        && departure.Reunion == (departure.Destinations.Count == 0);

    private static bool ValidActing(WorldDescriptor world, VillainPhaseFocusDescriptor acting, Known known)
    {
        if (acting.Step is not (1 or 2 or 4) || !known.Seats.Contains(acting.Seat)) return false;
        return world.Players.Single(player => player.Seat == acting.Seat).GameArea == acting.GameArea;
    }

    private static bool ValidNotes(IReadOnlyList<KangNoteDescriptor>? notes, Known known)
    {
        if (notes is null || !notes.All(note => note is not null && ValidNote(note, known))) return false;
        return notes.Select(note => (note.Kind, note.Card)).Distinct().Count() == notes.Count;
    }

    // The central-token and completion notes concern one scheme each; the others concern the table.
    private static bool ValidNote(KangNoteDescriptor note, Known known)
    {
        if (!Enum.IsDefined(note.Kind)) return false;
        bool anchored = note.Kind is KangNoteKind.CentralTokensWaitFor4A or KangNoteKind.CompletionStoresDominion;
        return anchored ? note.Card is { } card && known.Cards.ContainsKey(card) : note.Card is null;
    }

    private static bool Zone(WorldDescriptor world, int id, string zone) =>
        world.Areas.Count(area => area.Id == id && area.Owner == -1 && area.Zone == zone) == 1;
}
