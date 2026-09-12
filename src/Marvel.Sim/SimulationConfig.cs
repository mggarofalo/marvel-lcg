namespace Marvel.Sim;

internal sealed record SimulationConfig(
    string Scenario,
    string Difficulty,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<string>? ModularSets,
    int Games,
    IReadOnlyList<uint> ExplicitSeeds,
    uint? SeedStart,
    uint? SelectionSeed,
    uint PolicySeed,
    int DecisionLimit,
    string? Output,
    string? RepoRoot);
