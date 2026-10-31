using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Live absolute prohibitions on triggering Alter-Ego Actions on obligations.</summary>
public static class ObligationActionPermissions
{
    /// <summary>Engine-selected derived modifier, granted to each prohibited player's identity.</summary>
    public const string ProhibitedField = "obligation_alter_ego_action_prohibited";

    /// <summary>Whether <paramref name="player"/> may trigger an Alter-Ego Action on an obligation.</summary>
    /// <remarks>
    /// rr:cannot: "The word cannot is absolute, and cannot be countermanded
    /// by other abilities or effects."
    /// </remarks>
    public static bool CanTriggerAlterEgoAction(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        return player < 0 || player >= world.Seats.Count
            || StateFields.Modified(world, world.Seats[player].IdentityCard, ProhibitedField, world.Facts, world.Players) <= 0;
    }
}
