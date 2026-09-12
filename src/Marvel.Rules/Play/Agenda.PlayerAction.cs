using Marvel.Rules.Timing;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>How far through its three parts a step has got.</summary>
/// <remarks>
/// The parts are <c>rr:ability</c>'s: an interrupt window, the occurrence, a
/// response window. A step is in exactly one of them at any moment, which is
/// what makes the whole thing resumable.
/// </remarks>

/// <summary>An accepted player Action, stored as data until its agenda step applies.</summary>
/// <remarks>
/// The rules do not define an engine command format. Keeping the reconstructable
/// ability address, copied input lists and variable values here is the engine's
/// choice, and lets
/// an action survive a suspended interrupt window without retaining a call stack,
/// effect tree or session affordance handle.
/// </remarks>
public sealed record PlayerAction(
    PendingAbility Ability,
    IReadOnlyList<int> Paying,
    IReadOnlyList<int> Chosen,
    IReadOnlyDictionary<string, long>? Values = null,
    IReadOnlyList<ResourceAllocation>? Allocations = null)
{
    /// <summary>The numerical variables defined when this action was accepted.</summary>
    public IReadOnlyDictionary<string, long> DefinedValues => Values
        ?? EmptyValues;

    /// <summary>The accepted paid-icon allocations.</summary>
    public IReadOnlyList<ResourceAllocation> Allocated => Allocations ?? [];

    private static IReadOnlyDictionary<string, long> EmptyValues { get; } =
        new Dictionary<string, long>(StringComparer.Ordinal);
}
