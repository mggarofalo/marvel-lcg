using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Sim;

internal sealed record StepRecord(
    string Type,
    int Game,
    int Step,
    PromptRecord Prompt,
    DecisionSelector Decision,
    IReadOnlyList<int> Targets,
    IReadOnlyList<int> Resources,
    IReadOnlyDictionary<string, long> Values,
    IReadOnlyList<ResourceAllocation> Allocations,
    IReadOnlyList<JsonElement> Events,
    string Digest);
