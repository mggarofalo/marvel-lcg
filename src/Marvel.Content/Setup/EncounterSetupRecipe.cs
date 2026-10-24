namespace Marvel.Content.Setup;

/// <summary>Independent encounter groups in authored allocation and shuffle order.</summary>
public sealed record EncounterSetupRecipe(IReadOnlyList<EncounterGroupRecipe> Groups, int ActiveGroup);
