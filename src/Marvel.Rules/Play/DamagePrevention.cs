using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>The active damage-prevention effects and their shared amount calculation.</summary>
public static class DamagePrevention
{
    /// <summary>Prevention in the same order used by live damage placement.</summary>
    public static IEnumerable<ContinuousEffect> Active(World world, Card target) =>
        world.Effects.Active().Where(effect =>
            string.Equals(effect.Kind, "preventDamage", StringComparison.Ordinal)
            && effect.Affects == target.ObjectId);

    /// <summary>Apply one prevention amount without consuming its live registration.</summary>
    public static long After(long amount, long prevention) =>
        Math.Max(0, amount - (prevention <= 0 ? amount : prevention));
}
