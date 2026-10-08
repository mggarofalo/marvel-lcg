namespace Marvel.View;

/// <summary>A complete supported ability description, not an offer or permission to activate it.</summary>
public sealed record CardPersistentAbilityDescriptor(CardPersistentTriggerDescriptor Trigger, IReadOnlyList<CardPersistentCostDescriptor> Costs, IReadOnlyList<CardPersistentEffectDescriptor> Effects);
