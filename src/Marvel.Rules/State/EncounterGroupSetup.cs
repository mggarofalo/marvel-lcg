namespace Marvel.Rules.State;

/// <summary>Physical creation IDs for one Wrecking Crew villain group.</summary>
public sealed record EncounterGroupSetup(
    IReadOnlyList<int> Stages, int Scheme, IReadOnlyList<int> Encounters);
