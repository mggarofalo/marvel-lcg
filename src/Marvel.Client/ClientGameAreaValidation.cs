using Marvel.View;

namespace Marvel.Client;

/// <summary>Checks game-area summaries, seat reach and Kang facts before they become client state.</summary>
/// <remarks>
/// Each check is structural: a referenced id must be one the same response lists. The client
/// never decides reach, departures or progress itself; it rejects a response that names
/// cards, seats or game areas it does not also describe.
/// </remarks>
internal static class ClientGameAreaValidation
{
    internal static bool Valid(WorldDescriptor world)
    {
        if (!ClientTableValidation.KnownGameAreas(world)) return false;
        var known = new Known(
            [.. world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
                .Where(card => card.Id is not null).Select(card => card.Id!.Value)],
            [.. world.Players.Select(player => player.Seat)],
            [.. world.GameAreas.Select(area => area.Id)]);
        if (known.Areas.Count != world.GameAreas.Count) return false;
        if (!world.GameAreas.All(area => ValidArea(area, known))) return false;
        if (!world.Players.All(player => Distinct(player.OutOfReach, known.Cards))) return false;
        return world.Kang is not { } kang || ClientKangValidation.Valid(world, kang, known);
    }

    private static bool ValidArea(GameAreaDescriptor area, Known known)
    {
        if (area.Label is null) return false;
        if (!Distinct(area.Villains, known.Cards) || !Distinct(area.MainSchemes, known.Cards)) return false;
        if (!Distinct(area.FormerSeats, known.Seats)) return false;
        if (!area.PlayAreas.All(play => play == -1 || known.Seats.Contains(play))) return false;
        return area.Central == area.PlayAreas.Contains(-1) && !(area.Retained && area.PlayAreas.Any(play => play >= 0));
    }

    internal static bool Distinct(IReadOnlyList<int>? ids, HashSet<int> known)
    {
        if (ids is null) return false;
        return ids.Distinct().Count() == ids.Count && ids.All(known.Contains);
    }

    internal sealed record Known(HashSet<int> Cards, HashSet<int> Seats, HashSet<int> Areas);
}
