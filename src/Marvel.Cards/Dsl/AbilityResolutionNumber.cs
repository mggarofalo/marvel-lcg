using System.Collections.Immutable;

namespace Marvel.Cards.Dsl;

/// <summary>Resolution-owned numeric bindings with fixed authored arguments.</summary>
public enum AbilityResolutionNumber
{
    /// <summary>The paid or selected amount for a basic power.</summary>
    PowerAmount,
    /// <summary>Completed attacks against the current player in the scheduled activation batch.</summary>
    ActivationAttacksAgainstYou,
    /// <summary>Printed boost icons on all cards discarded this way.</summary>
    PrintedBoostIconsDiscarded,
    /// <summary>All printed resource icons on cards discarded this way, without generator multipliers.</summary>
    PrintedResourceIconsDiscarded,
    /// <summary>Distinct printed resource types on cards discarded this way; wild is its own printed type.</summary>
    DistinctPrintedResourceTypesDiscarded,
    /// <summary>One plus printed boost icons on the last card discarded this way.</summary>
    TopEncounterDiscardBoostPlusOne,
}
