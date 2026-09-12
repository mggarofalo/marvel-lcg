using System.Collections.Immutable;

namespace Marvel.Cards.Dsl;

/// <summary>A checked cardinality for a cost's player selection.</summary>
public abstract record AbilityCostRange
{
    private AbilityCostRange() { }
    /// <summary>Exactly this many cards.</summary>
    public sealed record Exact(int Count) : AbilityCostRange;
    /// <summary>At least one and no more than this many cards.</summary>
    public sealed record UpTo(int Count) : AbilityCostRange;
    /// <summary>Any positive number of available cards.</summary>
    public sealed record Any : AbilityCostRange;
}
