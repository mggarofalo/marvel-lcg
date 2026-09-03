namespace Marvel.Rules.Play;

/// <summary>A card-defined pool of all-purpose counters placed as it enters play.</summary>
/// <param name="Type">The name the card gives the counters.</param>
/// <param name="Starting">The positive number placed on entry.</param>
/// <param name="Uses">Whether the pool comes from the Uses keyword.</param>
public sealed record CardCounterPool(string Type, int Starting, bool Uses);
