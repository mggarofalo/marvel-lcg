namespace Marvel.View;

/// <summary>One authorized live persistent source. Contributions are already included in recipient totals.</summary>
public sealed record CardPersistentDescriptor(CardValueSourceDescriptor Source, CardRelationDescriptor Relation, IReadOnlyList<CardContributionDescriptor> Contributions, IReadOnlyList<CardPersistentAbilityDescriptor> Abilities, bool HasUnresolvedAbilities);
