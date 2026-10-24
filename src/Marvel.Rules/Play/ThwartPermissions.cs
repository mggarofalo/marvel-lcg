using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Character-specific prohibitions on performing thwarts.</summary>
public static class ThwartPermissions
{
    /// <summary>Engine-selected derived modifier for an absolute thwart prohibition.</summary>
    public const string ProhibitedField = "thwart_prohibited";

    /// <summary>Whether this character may perform a basic or labeled thwart.</summary>
    public static bool CanThwart(World world, Card character) =>
        StateFields.Modified(world, character, ProhibitedField, world.Facts, world.Players) <= 0;

    /// <summary>Refuses prohibited thwarts before costs or status cancellation.</summary>
    public static void Require(World world, Card character)
    {
        // rr:cannot: "The word cannot is absolute, and cannot be countermanded
        // by other abilities or effects." A Confused status does not grant permission.
        if (!CanThwart(world, character))
            throw new RulesNotImplementedException($"card {character.ObjectId} cannot thwart");
    }
}
