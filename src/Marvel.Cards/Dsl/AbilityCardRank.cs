using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Values by which an authored selector can rank cards.</summary>
public enum AbilityCardRank
{
    /// <summary>Printed cost.</summary>
    Cost,
    /// <summary>Modified attack value.</summary>
    Attack,
    /// <summary>Printed health, including a facedown card's rules-defined base.</summary>
    PrintedHealth,
}
