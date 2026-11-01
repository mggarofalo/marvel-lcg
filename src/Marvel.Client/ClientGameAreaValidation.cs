using Marvel.View;

namespace Marvel.Client;

/// <summary>Checks game-area summaries, seat reach and Kang facts before they become client state.</summary>
/// <remarks>
/// Each check is structural: a referenced id must be one the same response lists, and facts
/// that describe the same placement must agree with each other. The client never decides
/// reach, departures or progress itself.
/// </remarks>
internal static class ClientGameAreaValidation
{
    internal static bool Valid(WorldDescriptor world)
    {
        if (!ClientTableValidation.KnownGameAreas(world)) return false;
        var cards = world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id is not null).GroupBy(card => card.Id!.Value)
            .ToDictionary(group => group.Key, group => group.First().Location?.GameArea);
        var known = new Known(cards, [.. world.Players.Select(player => player.Seat)],
            [.. world.GameAreas.Select(area => area.Id)]);
        if (known.Areas.Count != world.GameAreas.Count) return false;
        if (!world.GameAreas.All(area => ValidArea(area, known))) return false;
        if (!world.Players.All(player => ValidReach(player, known))) return false;
        return world.Kang is not { } kang || ClientKangValidation.Valid(world, kang, known);
    }

    private static bool ValidArea(GameAreaDescriptor area, Known known)
    {
        if (area.Label is null || !Located(area.Villains, area.Id, known) || !Located(area.MainSchemes, area.Id, known))
            return false;
        if (!Distinct(area.FormerSeats, known.Seats)) return false;
        if (!area.PlayAreas.All(play => play == -1 || known.Seats.Contains(play))) return false;
        return area.Central == area.PlayAreas.Contains(-1) && !(area.Retained && area.PlayAreas.Any(play => play >= 0));
    }

    // A card a seat cannot reach is in some other game area than the seat's own; a seat in
    // no game area is limited by none.
    private static bool ValidReach(PlayerDescriptor player, Known known)
    {
        if (!Distinct(player.OutOfReach, known.Cards.Keys)) return false;
        if (player.GameArea is null) return player.OutOfReach.Count == 0;
        return player.OutOfReach.All(id => known.Cards[id] != player.GameArea);
    }

    private static bool Located(IReadOnlyList<int>? ids, int area, Known known) =>
        Distinct(ids, known.Cards.Keys) && ids!.All(id => known.Cards[id] == area);

    internal static bool Distinct(IReadOnlyList<int>? ids, IReadOnlyCollection<int> known)
    {
        if (ids is null) return false;
        return ids.Distinct().Count() == ids.Count && ids.All(known.Contains);
    }

    /// <summary>Listed card ids with their game area, listed seats and listed game areas.</summary>
    internal sealed record Known(Dictionary<int, int?> Cards, HashSet<int> Seats, HashSet<int> Areas);
}
