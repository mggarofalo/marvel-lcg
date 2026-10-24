namespace Marvel.Content.Setup;

/// <summary>Printed stage, scheme and ordered encounter composition for one villain.</summary>
public sealed record EncounterGroupRecipe(
    IReadOnlyList<string> Stages, string Scheme, IReadOnlyList<string> Encounters);
