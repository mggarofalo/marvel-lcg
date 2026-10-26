using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Recognizes independently empty physical-status replacement or removal.</summary>
internal static class AbilityIndependentStatusChoice
{
    internal static bool HasLegalOption(AbilityEffect effect, AbilityAdmissionScope scope) =>
        !IsReplacementChoice(effect)
        || ((AbilityEffect.Choose)effect).Options.Any(option => OptionIsLegal(option, scope));

    private static bool IsReplacementChoice(AbilityEffect effect) =>
        effect is AbilityEffect.Choose options
        && options.Options.Length == 3
        && options.Options.All(option => option is AbilityEffect.ReplaceStatus
            { Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen } })
        && options.Options.Cast<AbilityEffect.ReplaceStatus>().Select(option => option.Status).Distinct().Count() == 3;

    internal static bool IsEmpty(AbilityEffect effect, AbilityAdmissionScope scope) =>
        (IsRemovalChoice(effect) || effect is AbilityEffect.ChooseCard
        {
            From: AbilityCardSelection.Query { Kind: AbilityCardQuery.StatusCards },
            Effect: AbilityEffect.Choose options,
        }
        && IsReplacementChoice(options))
        && LegalCardChoices(effect, scope).Count == 0;

    // rr:and.1 permits an independently ineffective heal after the component is discarded.
    // Only that sibling is omitted from admission; outer sequences retain their gates.
    internal static AbilityContinuationFacts ContinuationWithoutIndependentHeal(
        AbilityEffect.ChooseCard choice, AbilityContinuationFacts continuation) =>
        continuation with
        {
            Frames = [.. continuation.Frames.Select(frame =>
                frame is SimultaneousContinuationFrame simultaneous
                    && simultaneous.Parent.Effects.Contains(choice)
                    ? simultaneous with { Remaining = [.. simultaneous.Remaining.Where(index =>
                        simultaneous.Parent.Effects[index] is not AbilityEffect.Heal
                        { Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen } })] }
                    : frame)],
        };

    internal static bool IsRemovalChoice(AbilityEffect effect) =>
        effect is AbilityEffect.ChooseCard
        {
            From: AbilityCardSelection.StatusCardsOn,
            Effect: AbilityEffect.CardAction
            {
                Instruction: AbilityCardInstruction.Discard,
                Selection: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            },
        };
}
