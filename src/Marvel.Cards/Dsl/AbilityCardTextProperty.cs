using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Card text and state properties that conditions can compare.</summary>
public enum AbilityCardTextProperty
{
    /// <summary>A status card.</summary>
    Status,
    /// <summary>A live trait.</summary>
    Trait,
    /// <summary>The printed encounter set.</summary>
    Set,
    /// <summary>The printed title.</summary>
    Title,
}
