namespace Marvel.Rules.State;

/// <summary>Characteristic loss shared by traits, keywords, and ability fields.</summary>
public static class Characteristics
{
    /// <summary>The persisted continuous-effect prefix for a lost characteristic.</summary>
    public const string Lost = "lost:";

    /// <summary>Names an effect that makes a card lose <paramref name="characteristic"/>.</summary>
    public static string LossOf(string characteristic)
    {
        ArgumentException.ThrowIfNullOrEmpty(characteristic);
        return Lost + characteristic;
    }

    /// <summary>Whether a card currently functions without this characteristic.</summary>
    public static bool IsLost(World world, Card card, string characteristic)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentException.ThrowIfNullOrEmpty(characteristic);

        string loss = LossOf(characteristic);
        return world.Effects.Active().Any(effect =>
            string.Equals(effect.Kind, loss, StringComparison.Ordinal)
            && effect.AppliesTo(world, card));
    }
}
