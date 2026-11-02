using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Named facts that hold from the moment they are recorded until the round ends.</summary>
/// <remarks>
/// Printed 16088 Nebula: "The first Technique attachment revealed each round gains
/// surge." A card ability records that its event has happened this round, and a later
/// instance of the same ability reads the record. The record is a lasting effect with
/// no source card, so it outlives the card that made it, including a villain stage
/// change. The mark and effect spellings are engine choices.
/// </remarks>
public static class RoundMarks
{
    private const string Prefix = "roundMark:";

    /// <summary>Records <paramref name="mark"/> until the end of the round.</summary>
    public static void Record(World world, string mark)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrEmpty(mark);
        if (Has(world, mark)) return;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Prefix + mark,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
    }

    /// <summary>Whether <paramref name="mark"/> has been recorded this round.</summary>
    public static bool Has(World world, string mark)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrEmpty(mark);
        string kind = Prefix + mark;
        return world.Effects.Active().Any(effect => string.Equals(effect.Kind, kind, StringComparison.Ordinal));
    }
}
