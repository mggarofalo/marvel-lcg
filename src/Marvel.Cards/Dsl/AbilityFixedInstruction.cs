using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Instructions with fixed authored markers and no variable parameters.</summary>
public enum AbilityFixedInstruction
{
    /// <summary>Advance directly to the next main scheme without completing this one.</summary>
    AdvanceMainScheme,
    /// <summary>Cancel the current occurrence.</summary>
    CancelOccurrence,
    /// <summary>Cancel the revealed card's When Revealed effects.</summary>
    CancelWhenRevealed,
    /// <summary>Also resolve the attack against each other hero.</summary>
    AlsoAttackEachOtherHero,
    /// <summary>Make the current attack indirect.</summary>
    MakeAttackIndirect,
    /// <summary>Prevent all damage from the current enemy attack.</summary>
    PreventAttackDamage,
    /// <summary>Reveal the top encounter card.</summary>
    RevealTop,
    /// <summary>Place one acceleration token.</summary>
    PlaceAccelerationToken,
    /// <summary>Generate the resources printed on the top discarded card.</summary>
    GenerateTopDiscard,
    /// <summary>Ask to play an ally from a player's discard pile.</summary>
    MakeTheCall,
    /// <summary>Require an ally defender for the engaged player.</summary>
    RequireAllyDefender,
    /// <summary>Continuously gain the effective traits of every environment in play.</summary>
    GainEnvironmentTraits,
    /// <summary>Keep this side scheme in play while the enclosing constant applies.</summary>
    PreventSideSchemeDeparture,
    /// <summary>Discard every live environment except the triggering entrant.</summary>
    DiscardOtherEnvironments,
}
