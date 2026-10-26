using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Describes complete checked readiness instructions without resolving their targets.</summary>
internal static class AbilityReadinessDescription
{
    internal static string? Summary(AbilityEffect effect) => effect switch
    {
        // Copy is an engine presentation choice. The title is the checked public
        // instruction's reference, not a discovered target or an ownership claim.
        AbilityEffect.CardAction
        {
            Instruction: AbilityCardInstruction.Ready,
            Selection: AbilityCardSelection.Titled titled,
        } => $"Ready {titled.Title}.",
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
