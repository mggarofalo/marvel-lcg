using Marvel.Cards.Dsl;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Describes the admitted self-scoped, phase-bound blanking instruction.</summary>
internal static class AbilityPrintedTextDescription
{
    internal static bool IsPhaseBlanking(AbilityEffect effect) => effect is AbilityEffect.GrantField
    {
        Field: PrintedText.Blanked,
        Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This },
        Amount: AbilityNumber.Constant { Value: 1 },
        EachCard: false,
        Until: TimingPoints.EndOfPhase,
    };

    internal static string? Caption(AbilityEffect effect) => IsPhaseBlanking(effect)
        ? "Blank this card's printed text box until the end of the phase"
        : null;
}
