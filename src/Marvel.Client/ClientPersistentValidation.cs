using Marvel.View;

namespace Marvel.Client;

/// <summary>Validates persistent descriptions without interpreting game rules.</summary>
internal static class ClientPersistentValidation
{
    internal static bool Complete(CardPersistentDescriptor? persistent) => persistent is null
        || CompleteSource(persistent.Source) && CompleteRelation(persistent.Relation)
        && persistent.Contributions is not null && persistent.Contributions.All(CompleteContribution)
        && persistent.Abilities is not null && persistent.Abilities.All(CompleteAbility);

    private static bool CompleteSource(CardValueSourceDescriptor? source) =>
        source is not null && ClientCardValidation.CompleteSource(source)
        && source.CardId is >= 0;

    private static bool CompleteRelation(CardRelationDescriptor? relation) =>
        relation is not null && !string.IsNullOrWhiteSpace(relation.Kind)
        && relation.HostId is null or >= 0 && relation.Controller is null or >= 0;

    private static bool CompleteContribution(CardContributionDescriptor? contribution) =>
        contribution is not null && contribution.TargetId >= 0
        && !string.IsNullOrWhiteSpace(contribution.Attribute)
        && !string.IsNullOrWhiteSpace(contribution.Operation);

    private static bool CompleteAbility(CardPersistentAbilityDescriptor? ability) =>
        ability?.Trigger is not null && !string.IsNullOrWhiteSpace(ability.Trigger.Timing)
        && ability.Costs is not null && ability.Costs.All(CompleteCost)
        && ability.Effects is not null && ability.Effects.All(CompleteEffect);

    private static bool CompleteCost(CardPersistentCostDescriptor? cost) =>
        cost is not null && !string.IsNullOrWhiteSpace(cost.Operation)
        && !string.IsNullOrWhiteSpace(cost.Target);

    private static bool CompleteEffect(CardPersistentEffectDescriptor? effect) =>
        effect is not null && !string.IsNullOrWhiteSpace(effect.Operation)
        && !string.IsNullOrWhiteSpace(effect.Target)
        && (effect.Condition is null || CompleteThreshold(effect.Condition));

    private static bool CompleteThreshold(CardPersistentThresholdDescriptor threshold) =>
        !string.IsNullOrWhiteSpace(threshold.Target) && !string.IsNullOrWhiteSpace(threshold.Counter);
}
