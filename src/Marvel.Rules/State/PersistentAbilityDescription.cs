namespace Marvel.Rules.State;

/// <summary>Supported ability facts and whether inspection is needed for any remaining ability.</summary>
public sealed record PersistentAbilityDescription(IReadOnlyList<PersistentAbility> Abilities, bool HasUnresolvedAbilities, bool HasAttachmentInstruction = false);
