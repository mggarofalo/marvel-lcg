using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Display copy naming Kang's departing groups by their players and Kang.</summary>
/// <remarks>
/// The wording is the engine's choice. Players know one another by name, not by a
/// game-area number, so every Kang prompt names the seats and the Kang involved.
/// </remarks>
internal static class KangDepartureNames
{
    /// <summary>The players who move, or the eliminated players whose retained components move.</summary>
    internal static string Group(World world, KangDeparture departure)
    {
        string[] seats = [.. departure.Group.Where(area => area.IsPlayers)
            .Select(area => GameAreaFacts.SeatName(world, area.Player))];
        if (seats.Length > 0) return GameAreaFacts.Names(seats);
        var former = GameAreaFacts.Of(world, departure.Source).FormerSeats;
        return former.Count > 0
            ? $"{GameAreaFacts.Names([.. former.Select(seat => GameAreaFacts.SeatName(world, seat))])}'s retained components"
            : "the departing components";
    }

    /// <summary>Why this group leaves, and from where.</summary>
    /// <remarks>
    /// 11002–11005: Kang (II)'s defeat removes his stage and moves the group at the end of
    /// the phase. 11009b–11012b: a completed stage stores a Kang's Dominion under 4A rather
    /// than losing the game, then the group moves the same way.
    /// </remarks>
    internal static string Departure(World world, KangDeparture departure)
    {
        string kang = EffectiveCards.Title(world.Cards[departure.Defeated.Card], world.Facts);
        string cause = departure.CompletedScheme is { } scheme
            ? $"{kang}'s {EffectiveCards.Title(world.Cards[scheme.Card], world.Facts)} was completed, storing a Kang's Dominion under stage 4A"
            : $"{kang} was defeated";
        return $"{cause}, so {Group(world, departure)} leave{(departure.Group.Count(area => area.IsPlayers) == 1 ? "s" : "")} "
            + $"{GameAreaFacts.Of(world, departure.Source).Label} at the end of the phase.";
    }

    /// <summary>What Kang's delayed effect does at this phase ending.</summary>
    internal static string DueAt(World world, PhaseStep owner)
    {
        var parts = world.Agenda.KangTransitions.DueAt(owner).Select(item => Departure(world, item)).ToList();
        if (world.Agenda.KangSplit.IsBoundary(owner))
            parts.Insert(0, "Kang (I) was defeated, so each player forms their own game area at the end of the phase.");
        return string.Join(" ", parts);
    }
}
