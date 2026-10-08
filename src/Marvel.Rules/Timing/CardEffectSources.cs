using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Source snapshots associated with the effects actually being evaluated.</summary>
public static class CardEffectSources
{
    /// <summary>Reads the registered origin, or the current source of a derived constant.</summary>
    public static CardSourceSnapshot? For(World world, ContinuousEffect effect)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(effect);
        ContinuousEffectEntry? entry = world.Effects.entries.FirstOrDefault(candidate =>
            ReferenceEquals(candidate.Effect, effect));
        return entry is not null
            ? entry.Origin
            : effect.Source == EffectSource.ConstantAbility ? Capture(world, effect) : null;
    }

    internal static CardSourceSnapshot? Capture(World world, ContinuousEffect effect) =>
        effect.Card is int id && id >= 0 && id < world.Cards.Count
            ? CardSourceSnapshot.Capture(world.Cards[id], world.Facts)
            : null;
}
