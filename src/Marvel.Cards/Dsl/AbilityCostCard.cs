using System.Collections.Immutable;

namespace Marvel.Cards.Dsl;

/// <summary>Cards a cost can identify without a player choice.</summary>
public enum AbilityCostCard
{
    /// <summary>The ability's source.</summary>
    Source,
    /// <summary>The resolving player's identity.</summary>
    Identity,
}
