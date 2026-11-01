using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Projects each game area's summary and each seat's reach for an authorized scope.</summary>
internal static class GameAreaProjection
{
    /// <summary>Adds the engine's game-area summaries, seat reach and Kang facts to a filtered snapshot.</summary>
    internal static WorldDescriptor With(World world, WorldDescriptor visible, IReadOnlySet<int> addressable)
    {
        var areas = GameAreaFacts.Of(world).Select(summary => new GameAreaDescriptor(
            summary.Area.Id, [.. summary.Area.PlayAreas.Select(play => play.Player).Order()])
        {
            Villains = Visible(summary.Villains, addressable),
            MainSchemes = Visible(summary.MainSchemes, addressable),
            Central = summary.Central,
            Retained = summary.Retained,
            FormerSeats = summary.FormerSeats.ToArray(),
            Label = summary.Label,
        }).ToArray();
        var players = visible.Players.Select(player => player with
        {
            OutOfReach = OutOfReach(world, player.Seat, addressable),
        }).ToArray();
        var projected = visible with { GameAreas = areas, Players = players };
        return projected with { Kang = KangTableProjection.From(world, projected, addressable) };
    }

    private static int[] Visible(IReadOnlyList<int> ids, IReadOnlySet<int> addressable) =>
        [.. ids.Where(addressable.Contains)];

    // The engine answers reach (pack:mc11:game-areas: "Cards and components in one game
    // area cannot affect another game area"); View only lists the visible in-play cards
    // for which that answer is no.
    private static int[] OutOfReach(World world, int seat, IReadOnlySet<int> addressable) =>
        [.. world.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type) && addressable.Contains(card.ObjectId)
                && !Places.CanAffect(world, seat, card))
            .Select(card => card.ObjectId).Order()];
}
