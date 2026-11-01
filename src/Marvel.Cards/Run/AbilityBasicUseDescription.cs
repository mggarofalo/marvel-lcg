using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Describes occurrence-bound bonuses and unresolved resource conditions.</summary>
internal static class AbilityBasicUseDescription
{
    /// <summary>Describes <paramref name="effect"/>; <paramref name="trigger"/> names the basic use it answers.</summary>
    internal static string? Summary(AbilityEffect effect, string? trigger = null) => effect switch
    {
        AbilityEffect.BoostBasicUse { Overkill: true } => "Grant overkill to this basic attack only",
        AbilityEffect.BoostBasicUse { Variable: not null } =>
            "Add 1 to this basic power use for each boost icon on the discarded card",
        AbilityEffect.BoostBasicUse { MatchingAlly: false } bonus => Bonus(bonus.Amount, trigger),
        AbilityEffect.Conditional { Then: AbilityEffect.BoostBasicUse { Overkill: true }, Else: null } conditional
            when Condition(conditional.Test) is { } condition =>
            $"If {condition}, grant overkill to this basic attack only",
        AbilityEffect.Sequence sequence => Sequence(sequence, trigger),
        _ => null,
    };

    // The trigger names the power: a thwart-only interrupt boosts THW, a use-level
    // interrupt boosts whichever basic power is being used, and otherwise ATK.
    private static string Bonus(int amount, string? trigger) => trigger switch
    {
        BasicPowerTiming.HeroThwartInitiated => $"Add {amount} THW for this basic thwart only",
        BasicPowerTiming.HeroUseInitiated or BasicPowerTiming.AllyUseInitiated =>
            $"Add {amount} to this basic attack or thwart only",
        _ => $"Add {amount} ATK for this basic attack only",
    };

    private static string? Sequence(AbilityEffect.Sequence sequence, string? trigger)
    {
        string?[] parts = [.. sequence.Effects.Select(effect => Summary(effect, trigger))];
        return parts.Length == 0 || parts.Any(part => part is null) ? null : string.Join("; ", parts);
    }

    private static string? Condition(AbilityCondition condition) => condition switch
    {
        AbilityCondition.PaidWithResource paid => $"{Resource(paid.Resource)} resources were spent",
        AbilityCondition.Negated { Operand: AbilityCondition.Any any }
            when any.Operands.All(item => item is AbilityCondition.PaidWithResource) =>
            "no " + string.Join(", ", any.Operands.Cast<AbilityCondition.PaidWithResource>().Select(item => Resource(item.Resource)))
                + " resources were spent",
        _ => null,
    };

    internal static string Resource(char resource) => resource switch
    {
        'R' => "physical", 'B' => "mental", 'Y' => "energy", 'G' => "wild",
        _ => throw new InvalidOperationException("Unknown checked resource"),
    };
}
