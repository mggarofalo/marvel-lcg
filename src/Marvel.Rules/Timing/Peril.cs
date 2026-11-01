using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Who is resolving a card with the peril keyword — <c>rr:peril</c>.</summary>
/// <remarks>
/// <c>rr:peril.1</c>: "While a player is resolving this card, that player cannot consult other
/// players, and other players cannot trigger abilities." A revealed encounter card resolves in
/// its revealing player's revealing area, so a peril card there names who is resolving it for
/// every occurrence nested in its resolution, including damage it deals another player (ruling
/// 1a4413236d2a28d8).
/// </remarks>
public static class Peril
{
    /// <summary>The player resolving a peril card, or null when none is resolving.</summary>
    public static int? Resolver(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        foreach (var area in world.Areas)
        {
            // Equivalent mutant: dropping the player test. Encounter cards are revealed by players.
            if (area.Type != DeckType.RevealingArea || !area.PlayArea.IsPlayers) continue;
            if (area.Cards.Any(card => Has(world, card))) return area.PlayArea.Player;
        }
        return null;
    }

    /// <summary>Whether another player's card is kept from helping the player resolving a peril card.</summary>
    public static bool KeepsOut(World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        int controller = CardControl.ControllerOf(world, card);
        // Equivalent mutants: keeping out any player during any peril (only the resolving player
        // acts then, rr:peril.1) and dropping the scenario-control test (no admitted
        // scenario-controlled card has a resource ability).
        return Resolver(world) == player && controller >= 0 && controller != player;
    }

    internal static bool Has(World world, Card card) =>
        StateFields.Modified(world, card, "peril", world.Facts, world.Players) > 0;
}
