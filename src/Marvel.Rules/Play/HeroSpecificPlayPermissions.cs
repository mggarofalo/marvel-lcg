using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Live absolute prohibitions on a player playing hero-specific cards.</summary>
public static class HeroSpecificPlayPermissions
{
    /// <summary>Engine-selected derived modifier, granted to the prohibited player's identity.</summary>
    public const string ProhibitedField = "hero_specific_play_prohibited";

    /// <summary>Whether <paramref name="player"/> may play <paramref name="card"/> under this prohibition.</summary>
    /// <remarks>
    /// rr:cannot: "The word cannot is absolute, and cannot be countermanded by
    /// other abilities or effects."
    /// </remarks>
    public static bool CanPlay(World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        return player < 0 || player >= world.Seats.Count
            || !CardClassification.Is(world.Facts, card.FaceId, CardClassification.HeroSpecific)
            || StateFields.Modified(world, world.Seats[player].IdentityCard, ProhibitedField, world.Facts, world.Players) <= 0;
    }
}
