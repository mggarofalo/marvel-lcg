using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Read-only summaries of each game area, derived from where cards are.</summary>
/// <remarks>
/// <c>pack:mc11:game-areas</c>: "Cards and components in one game area cannot
/// affect another game area (with the exception of the text on stage 2B)." A
/// client must show which villain and main scheme belong to which group of
/// players, so the engine names them here rather than leaving a renderer to
/// assemble them from zones and owners. Each answer comes from
/// <see cref="Places.GameAreaOf(World, Card)"/>, which also covers an
/// eliminated player's retained components.
/// </remarks>
public static class GameAreaFacts
{
    /// <summary>One summary per game area, in game-area id order.</summary>
    public static IReadOnlyList<GameAreaSummary> Of(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return [.. world.GameAreas.OrderBy(area => area.Id).Select(area => Of(world, area))];
    }

    internal static GameAreaSummary Of(World world, GameArea area)
    {
        // Only villains enter a villain area; their attachments sit in hosted areas of their own.
        int[] villains = InArea(world, area, card => card.Area.Type == DeckType.VillainArea);
        int[] schemes = InArea(world, area, card => card.Area.Type == DeckType.MainSchemesArea);
        bool central = area.Contains(PlayArea.Villains);
        bool retained = !area.PlayAreas.Any(play => play.IsPlayers) && KangCentral.Remains(world, area);
        int[] former = [.. world.Cards
            .Where(card => ReferenceEquals(Places.GameAreaOf(world, card), area))
            .Select(card => Places.PlayAreaOf(card))
            .Where(play => play.IsPlayers && !area.Contains(play))
            .Select(play => play.Player).Distinct().Order()];
        return new(area, villains, schemes, central, retained, former,
            Label(world, area, villains, central, former));
    }

    private static int[] InArea(World world, GameArea area, Func<Card, bool> holds) =>
        [.. world.Cards.Where(card => holds(card) && ReferenceEquals(Places.GameAreaOf(world, card), area))
            .Select(card => card.ObjectId).Order()];

    // Display copy is the engine's choice. A game area is named by the seats in it,
    // since its numeric id means nothing at the table, then by its villains.
    private static string Label(World world, GameArea area, int[] villains, bool central, int[] former)
    {
        string[] seats = [.. area.PlayAreas.Where(play => play.IsPlayers).Select(play => play.Player)
            .Order().Select(seat => SeatName(world, seat))];
        string owner = central ? "the central game area"
            : seats.Length > 0 ? $"{Names(seats)}'s game area"
            : former.Length > 0 ? $"{Names([.. former.Select(seat => SeatName(world, seat))])}'s retained game area"
            : "an empty game area";
        return villains.Length == 0 ? owner
            : $"{owner} ({string.Join(", ", villains.Select(id => EffectiveCards.Title(world.Cards[id], world.Facts)))})";
    }

    // A seat the world has not created yet is named by its number, as View names it.
    internal static string SeatName(World world, int seat) =>
        seat < world.Seats.Count ? world.Seats[seat].Name : $"Player {seat + 1}";

    internal static string Names(IReadOnlyList<string> names) => names.Count switch
    {
        0 => "no player",
        1 => names[0],
        _ => $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}",
    };
}
