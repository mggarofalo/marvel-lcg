namespace Marvel.Rules.State;

/// <summary>Pinned Wrecking Crew component order resolved by the content dealer before allocation.</summary>
public sealed record EncounterSetupPlan(IReadOnlyList<EncounterGroupSetup> Groups, int ActiveVillain);
