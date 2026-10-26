using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Live absolute prohibitions on readying a card.</summary>
public static class ReadinessPermissions
{
    /// <summary>Engine-selected derived modifier for an absolute ready prohibition.</summary>
    public const string ProhibitedField = "ready_prohibited";

    /// <summary>Whether readying is permitted by the current continuous effects.</summary>
    public static bool CanReady(World world, Card card) =>
        StateFields.Modified(world, card, ProhibitedField, world.Facts, world.Players) <= 0;
}
