using System.Collections.Immutable;

namespace Marvel.Cards.Dsl;

/// <summary>Resolution-owned numeric bindings with fixed authored arguments.</summary>
public enum AbilityResolutionNumber
{
    /// <summary>The paid or selected amount for a basic power.</summary>
    PowerAmount,
    /// <summary>Physical cards discarded as this ability's costs or effects.</summary>
    CardsDiscarded,
    /// <summary>Completed attacks against the current player in the scheduled activation batch.</summary>
    ActivationAttacksAgainstYou,
    /// <summary>Damage dealt by the enemy attack completed in this occurrence.</summary>
    CompletedAttackDamageDealt,
    /// <summary>Counted boost icons on all cards discarded this way.</summary>
    BoostIconsDiscarded,
    /// <summary>Cards discarded this way with a star icon in their boost area.</summary>
    StarIconsDiscarded,
    /// <summary>All printed resource icons on cards discarded this way, without generator multipliers.</summary>
    PrintedResourceIconsDiscarded,
    /// <summary>Distinct printed resource types on cards discarded this way; wild is its own printed type.</summary>
    DistinctPrintedResourceTypesDiscarded,
    /// <summary>One plus the counted boost icons on the last card discarded this way.</summary>
    TopEncounterDiscardBoostPlusOne,
    /// <summary>Sum of the printed resource costs of cards discarded this way.</summary>
    PrintedCostDiscarded,
    /// <summary>Distinct printed card types among cards discarded this way.</summary>
    DistinctCardTypesDiscarded,
    /// <summary>Physical cards selected and exhausted for this ability's exhaustChosen cost.</summary>
    CardsExhaustedForCost,
    /// <summary>Actual damage taken by the resolving identity from this occurrence's enemy attack.</summary>
    ReceivedAttackDamage,
    /// <summary>Settled excess for the bound attack-end recipient.</summary>
    AttackExcessDamage,
    /// <summary>Resources generated beyond the exact source card's entry cost.</summary>
    EntryResourceExcess,
    /// <summary>Energy resources, including overpaid wild resources, generated beyond the source card's entry cost.</summary>
    EntryEnergyExcess,
    /// <summary>The damage the interrupted damage window's recipient would take after pending prevention.</summary>
    IncomingDamage,
    /// <summary>The damage the triggering attack dealt its bound recipient, whether or not it was taken.</summary>
    AttackDamageDealt,
}
