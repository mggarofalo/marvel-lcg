using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>Allocated cards and their optional independent-deck relationships before setup executes.</summary>
public sealed record ScenarioDealPlan(IReadOnlyList<CardBlueprint> Blueprints, EncounterSetupPlan? EncounterGroups);
