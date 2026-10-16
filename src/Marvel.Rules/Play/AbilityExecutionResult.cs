using Marvel.Rules.Events;

namespace Marvel.Rules.Play;

/// <summary>A checked return from an invocation, separate from its effects' success.</summary>
public sealed class AbilityExecutionResult
{
    internal AbilityExecutionResult(AbilityExecution invocation, IReadOnlyList<GameEvent> events)
    {
        Invocation = invocation;
        Events = events;
    }

    internal AbilityExecution Invocation { get; }

    /// <summary>Events emitted by this execution segment.</summary>
    public IReadOnlyList<GameEvent> Events { get; }

    /// <summary>Whether the invocation and its immediate work have completed.</summary>
    public bool IsComplete => Invocation.IsComplete;
}
