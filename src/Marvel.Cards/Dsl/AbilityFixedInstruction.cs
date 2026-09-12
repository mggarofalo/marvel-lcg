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
}
