using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Describes complete checked source-discard and identity-ready instructions.</summary>
internal static class AbilityReadinessDescription
{
    internal static string? Summary(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Sequence
        {
            Effects: [
                AbilityEffect.CardAction
                {
                    Instruction: AbilityCardInstruction.Discard,
                    Selection: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This },
                },
                AbilityEffect.CardAction
                {
                    Instruction: AbilityCardInstruction.Ready,
                    Selection: AbilityCardSelection.Bound { Binding: AbilityCardBinding.You },
                },
            ],
        } => "Discard this card, then ready your identity.",
        _ => null,
    };
}
