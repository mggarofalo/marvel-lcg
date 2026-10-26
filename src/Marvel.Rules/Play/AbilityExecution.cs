using Marvel.Rules.Events;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>One game-owned invocation, distinct from whether its effects applied.</summary>
public sealed class AbilityExecution
{
    internal AbilityExecution(Agenda agenda, Occurrence occurrence,
        PendingAbility? ability, long ownerFrame, AbilityExecution? parent)
    {
        Agenda = agenda;
        Occurrence = occurrence;
        Resolution = ability;
        OwnerFrame = ownerFrame;
        SegmentOwnerFrame = ownerFrame;
        Parent = parent;
    }

    internal Agenda Agenda { get; }
    /// <summary>The triggering occurrence retained throughout this invocation.</summary>
    public Occurrence Occurrence { get; }
    /// <summary>The admitted ability, including its exact triggering instance.</summary>
    public PendingAbility Ability => Resolution
        ?? throw new InvalidOperationException("an entry batch has no individual ability address");
    internal PendingAbility? Resolution { get; }
    /// <summary>Whether this scope owns entry work without changing an individual ability's semantic result.</summary>
    public bool IsBatch => Resolution is null;
    /// <summary>Whether an optional direct-damage window split this invocation's initial event receipt.</summary>
    public bool DamagePreventionSuspended { get; internal set; }
    internal long OwnerFrame { get; }
    internal long SegmentOwnerFrame { get; set; }
    internal AbilityExecution? Parent { get; }
    internal HashSet<long> Frames { get; } = [];
    internal HashSet<long> SegmentFrames { get; } = [];
    internal HashSet<AbilityExecution> Children { get; } = [];
    internal HashSet<AbilityCompletionObligation> CompletionObligations { get; } = [];
    internal AbilityExecutionState State { get; set; } = AbilityExecutionState.Running;
    internal bool DeferredActivation { get; set; }
    internal AbilityResponseBoundary? ResponseBoundary { get; set; }
    internal ResolvedPreparationAbility? Preparation { get; set; }
    internal bool ReadyToComplete => State == AbilityExecutionState.Completing
        && Frames.Count == 0 && Children.Count == 0 && CompletionObligations.Count == 0;
    internal bool WaitsNow => !DeferredActivation && State != AbilityExecutionState.Abandoned
        && (State is AbilityExecutionState.Running or AbilityExecutionState.Suspended
            || Frames.Count > 0 || CompletionObligations.Count > 0 || Children.Any(child => child.WaitsNow));

    /// <summary>Finish this segment without a suspended interpreter continuation.</summary>
    public AbilityExecutionResult Finish(IReadOnlyList<GameEvent> events)
    {
        Agenda.Executions.Complete(this);
        return Report(events);
    }

    /// <summary>Yield to registered work that must run before this invocation can continue.</summary>
    public AbilityExecutionResult Yield(IReadOnlyList<GameEvent> events)
    {
        Agenda.Executions.Suspend(this);
        return Report(events);
    }

    /// <summary>Publish events only after this segment has explicitly stopped running.</summary>
    public AbilityExecutionResult Report(IReadOnlyList<GameEvent> events) =>
        Agenda.Executions.Report(this, events);

    /// <summary>Completion includes all owned procedures and continuations, including deferred activations.</summary>
    public bool IsComplete => State == AbilityExecutionState.Complete;
}
