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
    /// <summary>Modified scheme value.</summary>
    Scheme,
    /// <summary>Printed health, including a facedown card's rules-defined base.</summary>
    PrintedHealth,
    /// <summary>Current maximum health minus damage, including active modifiers.</summary>
    RemainingHealth,
    /// <summary>Current threat on a scheme.</summary>
    Threat,
}
