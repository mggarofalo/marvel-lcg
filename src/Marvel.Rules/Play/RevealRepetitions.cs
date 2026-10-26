using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Reads authored constant modifiers for a player's encounter revelation.</summary>
public static class RevealRepetitions
{
    /// <summary>Engine spelling of additional resolutions of each When Revealed ability.</summary>
    public const string Modifier = "additionalWhenRevealed";

    internal static int Copies(World world, int player)
    {
        // ruling:c516c82788ea8d77: a villain stage is not revealed by a specific player.
        if (player < 0 || player >= world.Seats.Count) return 1;
        int identity = world.Seats[player].IdentityCard.ObjectId;
        long additional = world.Effects.Active().Where(effect =>
            effect.Kind == Modifier && effect.Affects == identity).Sum(effect => effect.Amount);
        return checked(1 + (int)Math.Max(0, additional));
    }
}
