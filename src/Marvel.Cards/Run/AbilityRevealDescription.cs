using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Describes complete checked encounter cancellation effects without reading the deck.</summary>
internal static class AbilityRevealDescription
{
    internal static string? Summary(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.CancelWhenRevealed } =>
            "Cancel the revealed treachery's When Revealed effects",
        AbilityEffect.Sequence
        {
            Effects: [
                AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.CancelWhenRevealed },
                AbilityEffect.CardAction
                {
                    Instruction: AbilityCardInstruction.Discard,
                    Selection: AbilityCardSelection.Bound { Binding: AbilityCardBinding.TriggerSubject },
                },
                AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.RevealTop },
            ],
        } => "Cancel the revealed encounter card's effects and discard it, then reveal the next encounter card. "
            + "The replacement card's effects remain unresolved.",
        _ => null,
    };
}
