using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Names a public quantity before an offered option opens its separate target choice.</summary>
internal static class AbilityTargetedOptionDescription
{
    internal static (string? Label, string? Description)? From(
        AbilityStructuralContext context, AbilityEffect option) => option switch
    {
        AbilityEffect.ChooseCard { Effect: AbilityEffect.RemoveThreat threat } choice
            when AbilityPublicAmounts.IsFixed(threat.Amount) => Describe(context, choice, threat.Amount, "Remove", "threat"),
        AbilityEffect.ChooseCard { Effect: AbilityEffect.Damage damage } choice
            when AbilityPublicAmounts.IsFixed(damage.Amount) => Describe(context, choice, damage.Amount, "Deal", "damage"),
        _ => null,
    };

    private static (string Label, string? Description) Describe(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, AbilityNumber number,
        string verb, string unit) =>
        ($"{verb} {AbilityStructuralQueries.Amount(number, context.Expressions)} {unit}",
            AbilityEffectDescription.Summary(choice));
}
