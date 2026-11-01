using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic Rocket Raccoon boards over the production ability book.</summary>
internal static class RocketBoards
{
    internal const string Identity = "16029a,16029b";
    internal const string Energy = "01088";
    internal const string Genius = "01089";
    internal const string Strength = "01090";

    /// <summary>Rocket Raccoon on <paramref name="face"/>, otherwise <see cref="GrootBoards.Board"/>.</summary>
    internal static World Board(string face = "16029a", int players = 1, string encounter = "01104", string villain = "01094") =>
        GrootBoards.Board(face, players, encounter, villain, Identity);

    internal static Card Rocket(World world) => world.Seats[0].IdentityCard;

    /// <summary>The charge counters on <paramref name="card"/>.</summary>
    internal static long Charge(Card card) => card.Tokens.GetValueOrDefault("c_charge");

    /// <summary>An upgrade in Rocket's play area holding <paramref name="charge"/> charge counters.</summary>
    internal static Card Upgrade(World world, string face, int charge = 0, int player = 0)
    {
        var card = GrootBoards.InPlay(world, face, DeckType.UpgradesArea, player);
        if (charge > 0) card.PlaceTokens("c_charge", charge);
        return card;
    }

    /// <summary>Begins a basic attack by Rocket against <paramref name="target"/>.</summary>
    internal static void BasicAttack(World world, Card target) =>
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, target, []);
}
