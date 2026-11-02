namespace Marvel.Rules.Play;

/// <summary>A card-defined pool of all-purpose counters placed as it enters play.</summary>
/// <param name="Type">The name the card gives the counters.</param>
/// <param name="Starting">The positive number placed on entry.</param>
/// <param name="Uses">Whether the pool comes from the Uses keyword.</param>
public sealed record CardCounterPool(string Type, int Starting, bool Uses)
{
    /// <summary>The printed count is multiplied by the original player count.</summary>
    public bool PerPlayer { get; init; }

    /// <summary>Counters placed in addition to the printed count, never multiplied.</summary>
    /// <remarks>Fanaticism (16110) prints "1 fury counter, plus 1 per hero additional fury counters".</remarks>
    public int Additional { get; init; }

    /// <summary>The counters this pool places when its card enters play with <paramref name="players"/> players.</summary>
    public long StartingFor(int players) => checked((long)Starting * (PerPlayer ? players : 1) + Additional);
}
