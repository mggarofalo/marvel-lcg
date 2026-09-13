using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Sim;

internal sealed record HeaderRecord(
    string Type,
    int Schema,
    string Scenario,
    string Difficulty,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<string>? ModularSets,
    string Policy,
    int PolicyVersion,
    string PolicyVisibility,
    uint PolicySeed,
    int DecisionLimit,
    string SeedMode,
    uint? SelectionSeed,
    IReadOnlyList<uint> Seeds);
