namespace Marvel.Rules.State;

/// <summary>A passive description of one current source, before visibility filtering.</summary>
public sealed record PersistentCardDescription(CardSourceSnapshot Source, PersistentCardRelation Relation, IReadOnlyList<PersistentAbility> Abilities, bool HasUnresolvedAbilities);
