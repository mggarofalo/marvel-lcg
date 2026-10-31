using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Describes occurrence-bound bonuses and unresolved resource conditions.</summary>
internal static class AbilityBasicUseDescription
{
    internal static string? Summary(AbilityEffect effect) => effect switch
    {
        AbilityEffect.BoostBasicUse { Overkill: true } => "Grant overkill to this basic attack only",
        AbilityEffect.BoostBasicUse { Variable: not null } =>
            "Add 1 to this basic power use for each boost icon on the discarded card",
        AbilityEffect.BoostBasicUse { MatchingAlly: false } bonus => $"Add {bonus.Amount} ATK for this basic attack only",
        AbilityEffect.Conditional { Then: AbilityEffect.BoostBasicUse { Overkill: true }, Else: null } conditional
            when Condition(conditional.Test) is { } condition =>
            $"If {condition}, grant overkill to this basic attack only",
        AbilityEffect.Sequence sequence => Sequence(sequence),
        _ => null,
    };

    private static string? Sequence(AbilityEffect.Sequence sequence)
    {
        string?[] parts = [.. sequence.Effects.Select(Summary)];
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
