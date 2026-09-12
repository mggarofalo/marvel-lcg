using System.Collections.Immutable;

namespace Marvel.Cards.Dsl;

/// <summary>Card values that numeric expressions can read.</summary>
public enum AbilityCardNumberProperty
{
    /// <summary>Threat tokens.</summary>
    Threat,
    /// <summary>Damage tokens.</summary>
    Damage,
    /// <summary>Modified health less damage, bounded below by zero.</summary>
    RemainingHealth,
    /// <summary>The identity's printed starting health.</summary>
    StartingHealth,
}
