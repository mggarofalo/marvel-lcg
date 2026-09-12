using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Labels with engine-owned power resolution.</summary>
public enum AbilityPowerKind
{
    /// <summary>An attack ability.</summary>
    Attack,
    /// <summary>A defense ability.</summary>
    Defense,
    /// <summary>A thwart ability.</summary>
    Thwart,
}
