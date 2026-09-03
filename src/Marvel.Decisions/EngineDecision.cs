using System.Runtime.CompilerServices;
using Marvel.Rules.Play;

[assembly: InternalsVisibleTo("Marvel.Server")]

namespace Marvel.Decisions;

/// <summary>The five fields the engine accepts as a decision, and no derived properties.</summary>
/// <remarks>
/// This DTO is separate from <see cref="Decision"/> because that domain type
/// also exposes convenience getters such as <c>Spent</c>. Those getters are not
/// additional wire fields. The spelling here is an engine choice, not a rule.
/// </remarks>
public sealed record EngineDecision(
    int Affordance,
    IReadOnlyList<int> Targets,
    IReadOnlyList<int>? Resources = null,
    IReadOnlyDictionary<string, long>? Values = null,
    IReadOnlyList<ResourceAllocation>? Allocations = null)
{
    internal Decision ToDomain() =>
        new(Affordance, Targets, Resources, Values, Allocations);

    /// <summary>The wire form of a decline.</summary>
    public static EngineDecision Decline { get; } = new(-1, []);
}
