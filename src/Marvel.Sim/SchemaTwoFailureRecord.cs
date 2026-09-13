using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Sim;

internal sealed record SchemaTwoFailureRecord(
    string Type,
    string Category,
    int Game,
    uint Seed,
    int Step,
    int Round,
    PolicyMetrics Metrics,
    string Exception,
    string Message,
    JsonElement? Prompt,
    DecisionSelector? Decision,
    IReadOnlyList<int> Targets,
    IReadOnlyList<int> Resources,
    IReadOnlyDictionary<string, long> Values,
    IReadOnlyList<ResourceAllocation> Allocations,
    string? LastGoodDigest,
    string? PostFailureDigest,
    IReadOnlyList<SchemaTwoStepRecord> RecentSteps,
    string Reproduce);
