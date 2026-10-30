using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Instructions with fixed authored markers and no variable parameters.</summary>
public enum AbilityFixedInstruction
{
    /// <summary>The players win the game.</summary>
    PlayersWin,
    /// <summary>Scenario-owned captureKangStageOneDefeat instruction.</summary>
    CaptureKangStageOneDefeat,
    /// <summary>Scenario-owned captureKangStageTwoDefeat instruction.</summary>
    CaptureKangStageTwoDefeat,
    /// <summary>Store Dominion and retain the completed local stage until phase end.</summary>
    CaptureKangLocalCompletion,
    /// <summary>Scenario-owned splitKangStageTwo instruction.</summary>
    SplitKangStageTwo,
    /// <summary>Scenario-owned createKangGameArea instruction.</summary>
    CreateKangGameArea,
    /// <summary>Reveal the final Kang stage from 4A.</summary>
    RevealKangStageThree,
    /// <summary>Resolve 4B nemesis searches and entries.</summary>
    PutKangNemesesIntoPlay,
    /// <summary>Advance directly to the next main scheme without completing this one.</summary>
    AdvanceMainScheme,
    /// <summary>Use ATK instead of THW for the current thwart.</summary>
    UseAttackForThwart,
    /// <summary>Ready the captured defending hero if that attack causes it no damage.</summary>
    ReadyUndamagedDefender,
    /// <summary>Cancel the current occurrence.</summary>
    CancelOccurrence,
    /// <summary>Replace the imminent ready of this attachment's host.</summary>
    ReplaceReady,
    /// <summary>Cancel numeric icons on the exact faceup boost card.</summary>
    CancelBoostIcons,
    /// <summary>Cancel the ability on the exact faceup boost card.</summary>
    CancelBoostAbility,
    /// <summary>Cancel the revealed card's When Revealed effects.</summary>
    CancelWhenRevealed,
    /// <summary>Also resolve the attack against each other hero.</summary>
    AlsoAttackEachOtherHero,
    /// <summary>Make the current attack indirect.</summary>
    MakeAttackIndirect,
    /// <summary>Prevent all damage from the current enemy attack.</summary>
    PreventAttackDamage,
    /// <summary>Reflect damage to this attachment host during the active player attack.</summary>
    ReflectAttackDamage,
    /// <summary>Reveal the top encounter card.</summary>
    RevealTop,
    /// <summary>Place one acceleration token.</summary>
    PlaceAccelerationToken,
    /// <summary>Generate the resources printed on the top discarded card.</summary>
    GenerateTopDiscard,
    /// <summary>Ask to play an ally from a player's discard pile.</summary>
    MakeTheCall,
    /// <summary>Choose a shared-trait permanent to play at a reduced cost.</summary>
    PlaySharedHeroTraitCard,
    /// <summary>Require an ally defender for the engaged player.</summary>
    RequireAllyDefender,
    /// <summary>Require the attacked player's legal ally during this activation.</summary>
    RequireAttackedPlayerAllyDefender,
    /// <summary>Continuously gain the effective traits of every environment in play.</summary>
    GainEnvironmentTraits,
    /// <summary>Blank each in-play TECH player card.</summary>
    BlankTechPlayerCards,
    /// <summary>Keep this side scheme in play while the enclosing constant applies.</summary>
    PreventSideSchemeDeparture,
    /// <summary>Discard every live environment except the triggering entrant.</summary>
    DiscardOtherEnvironments,
    /// <summary>Privately inspect the top card of one chosen deck until acknowledgement.</summary>
    PeekAnyDeck,
}
