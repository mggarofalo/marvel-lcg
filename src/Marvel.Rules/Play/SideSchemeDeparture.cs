using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Printed restrictions that keep a side scheme in play.</summary>
public static class SideSchemeDeparture
{
    /// <summary>Engine-chosen continuous-effect key for an absolute departure prohibition.</summary>
    public const string Prohibited = "cannotLeavePlay";

    /// <summary>Whether this scheme may leave its current area.</summary>
    public static bool Allowed(World world, Card card) => card.Area.Type != DeckType.SideSchemesArea
        || !world.Effects.Active().Any(effect => effect.Kind == Prohibited && effect.Affects == card.ObjectId);
}
