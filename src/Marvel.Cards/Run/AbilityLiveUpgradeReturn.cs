using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;

namespace Marvel.Cards.Run;

/// <summary>Recognizes a power's final upgrade return that resolves on the live board after opaque work.</summary>
/// <remarks>
/// rr:ability.4: "resolve the ability one sentence at a time", and
/// rr:when-defeated-abilities.2.1: "A defeated card leaves play after its 'When Defeated'
/// ability is resolved", so a defeat window the damage opens finishes before the return
/// begins (ruling:cef917d276a31694: "first the enemy is dealt 4 damage, then the enemy's
/// 'When Defeated' resolves and deals 1 damage, then the hero gets a tough status card").
/// The return chooses nothing: it finds its card by title when it resolves, so its
/// result is the live board's, whatever the opaque work did.
/// </remarks>
internal static class AbilityLiveUpgradeReturn
{
    internal static bool CanFinish(AbilityEffect node, AbilityAdmissionScope cast,
        bool terminal, IEnumerable<AbilityPowerState> paths)
    {
        if (!terminal || !paths.All(HidesLiveWork)) return false;
        if (node is not AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.ReturnToHand,
              Selection: AbilityCardSelection.Titled } returned) return false;
        var cards = Every(returned.Selection, cast).ToArray();
        if (cards.Length != 1 || cards[0].Owner != cast.Player
            || EffectiveCards.Kind(cards[0], cast.World.Facts) != CardKind.Upgrade
            || !DeckTypes.IsInPlay(cards[0].Area.Type)) return false;
        // No later abstract state consumes this departure. The live instruction
        // reselects after stage or defeat-window work, can find no card, and owns
        // constant endings and their defeat settlement. No stage result, defeat
        // result or hidden pile is read.
        cast.World.Effects.PreflightConstantsEnding(cards[0]);
        return true;
    }

    // A step-6 recovery keeps its minion in play; its own read proof decides that path.
    private static bool HidesLiveWork(AbilityPowerState path) =>
        path.UnprojectedStageEntry is not null
        || path.DefeatWindowDeferred && !path.RecoveryWindowDeferred;
}
