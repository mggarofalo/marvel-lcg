using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;

namespace Marvel.Cards.Run;

/// <summary>Recognizes a power's final upgrade return that resolves on the live board after opaque work.</summary>
internal static class AbilityLiveUpgradeReturn
{
    internal static bool CanFinish(AbilityEffect node, AbilityAdmissionScope cast,
        bool terminal, IEnumerable<AbilityPowerState> paths)
    {
        if (!terminal || paths.Any(path => path.UnprojectedStageEntry is null)) return false;
        if (node is not AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.ReturnToHand,
              Selection: AbilityCardSelection.Titled } returned) return false;
        var cards = Every(returned.Selection, cast).ToArray();
        if (cards.Length != 1 || cards[0].Owner != cast.Player
            || EffectiveCards.Kind(cards[0], cast.World.Facts) != CardKind.Upgrade
            || !DeckTypes.IsInPlay(cards[0].Area.Type)) return false;
        // No later abstract state consumes this departure. The live instruction
        // reselects after stage work, can find no card, and owns constant endings
        // and their defeat settlement. No stage result or hidden pile is read.
        cast.World.Effects.PreflightConstantsEnding(cards[0]);
        return true;
    }
}
