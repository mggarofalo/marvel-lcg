using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Admits only the independently empty physical-status replacement sentence.</summary>
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
        effect is AbilityEffect.ChooseCard
        {
            From: AbilityCardSelection.Query { Kind: AbilityCardQuery.StatusCards },
            Effect: AbilityEffect.Choose options,
        }
        && IsReplacementChoice(options)
        && LegalCardChoices(effect, scope).Count == 0;
}
