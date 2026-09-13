using System.Collections.Immutable;

namespace Marvel.Cards.Dsl;

/// <summary>Resolution-owned numeric bindings with fixed authored arguments.</summary>
public enum AbilityResolutionNumber
{
    /// <summary>The paid or selected amount for a basic power.</summary>
    PowerAmount,
    /// <summary>Printed boost icons on all cards discarded this way.</summary>
    PrintedBoostIconsDiscarded,
    /// <summary>One plus printed boost icons on the last card discarded this way.</summary>
    TopEncounterDiscardBoostPlusOne,
}
