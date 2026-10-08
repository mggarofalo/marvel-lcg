namespace Marvel.Rules.State;

/// <summary>A complete supported ability description, independent of present action legality.</summary>
public sealed record PersistentAbility(PersistentTrigger Trigger, IReadOnlyList<PersistentCost> Costs, IReadOnlyList<PersistentEffect> Effects);
