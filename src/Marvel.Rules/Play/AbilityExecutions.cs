using Marvel.Rules.Events;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The agenda's invocation ownership and completion transitions.</summary>
public sealed class AbilityExecutions
{
    private readonly Agenda agenda;
    private readonly List<AbilityExecution> invocations = [];
    private readonly Stack<AbilityExecution> running = [];
    private AbilityExecution? procedureOwner;
    private long? procedureFrame;

    internal AbilityExecutions(Agenda agenda) => this.agenda = agenda;
    internal object Identity { get; } = new();
    internal long SchedulingFrame => Active?.SegmentOwnerFrame ?? procedureFrame ?? agenda.Current?.FrameId ?? 0;
    internal AbilityExecution? Active => running.TryPeek(out var active) ? active : null;

    internal IDisposable EnterProcedure(PhaseStep step)
    {
        if (step.CompletionPrerequisite is { } prerequisite)
        {
            RequireOwned(prerequisite);
            if (!prerequisite.IsComplete)
                throw new InvalidOperationException("the procedure's prerequisite execution is unfinished");
        }
        agenda.StartProcedureScheduling(step);
        var previous = procedureOwner;
        var previousFrame = procedureFrame;
        procedureOwner = step.ExecutionOwner;
        procedureFrame = step.FrameId;
        return new ProcedureScope(this, previous, previousFrame);
    }

    private sealed class ProcedureScope(AbilityExecutions executions, AbilityExecution? previous,
        long? previousFrame) : IDisposable
    {
        public void Dispose()
        {
            var ordered = AbilityWorkOrdering.ByDependencies([.. executions.agenda.items], executions);
            executions.agenda.items.Clear();
            executions.agenda.items.AddRange(ordered);
            executions.procedureOwner = previous;
            executions.procedureFrame = previousFrame;
        }
    }

    /// <summary>Invoke one resolver and verify it returned this game's stopped invocation.</summary>
    public AbilityExecutionResult Run(Occurrence occurrence, PendingAbility ability,
        Func<AbilityExecution, AbilityExecutionResult> resolve)
    {
        return Invoke(Begin(occurrence, ability), resolve);
    }

    /// <summary>Admit a card entry before its ordered individual abilities or procedures run.</summary>
    public AbilityExecutionResult RunBatch(Occurrence occurrence, Func<AbilityExecution, AbilityExecutionResult> resolve) =>
        Invoke(BeginBatch(occurrence), resolve);

    /// <summary>Validate that a card entry uses this game's active batch, not an individual ability handle.</summary>
    public void RequireBatch(AbilityExecution entry)
    {
        RequireActive(entry);
        if (entry.Resolution is not null)
            throw new InvalidOperationException("a card entry requires a batch execution");
    }

    /// <summary>Finish a card entry after all synchronous work has been registered.</summary>
    public AbilityExecutionResult FinishBatch(AbilityExecution entry, IReadOnlyList<GameEvent> events)
    {
        RequireBatch(entry);
        return entry.Finish(events);
    }

    private AbilityExecutionResult Invoke(AbilityExecution invocation, Func<AbilityExecution, AbilityExecutionResult> resolve)
    {
        try
        {
            var result = resolve(invocation);
            if (result.Invocation != invocation)
                throw new InvalidOperationException("the resolver returned another invocation");
            RequireStopped(invocation);
            return result;
        }
        catch
        {
            RejectUnstarted(invocation);
            throw;
        }
    }

    private void RejectUnstarted(AbilityExecution invocation)
    {
        if (Active != invocation || invocation.Frames.Count > 0 || invocation.Children.Count > 0) return;
        running.Pop();
        invocation.State = AbilityExecutionState.Abandoned;
        invocations.Remove(invocation);
        invocation.Parent?.Children.Remove(invocation);
    }

    internal AbilityExecutionResult Report(AbilityExecution invocation, IReadOnlyList<GameEvent> events)
    {
        RequireStopped(invocation);
        return new(invocation, events);
    }

    private void RequireStopped(AbilityExecution invocation)
    {
        RequireOwned(invocation);
        if (running.Contains(invocation) || invocation.State == AbilityExecutionState.Running)
            throw new InvalidOperationException("the resolver did not complete or suspend its invocation");
    }

    /// <summary>Admit an invocation before its costs or effects can create nested work.</summary>
    public AbilityExecution Begin(Occurrence occurrence, PendingAbility ability)
        => BeginCore(occurrence, ability);

    /// <summary>Start an entry or delayed effect scope without reopening an individual ability's semantic result.</summary>
    public AbilityExecution BeginBatch(Occurrence occurrence) => BeginCore(occurrence, null);

    private AbilityExecution BeginCore(Occurrence occurrence, PendingAbility? ability)
    {
        var invocation = new AbilityExecution(agenda, occurrence,
            ability, SchedulingFrame, Active ?? procedureOwner);
        invocation.Parent?.Children.Add(invocation);
        invocations.Add(invocation);
        running.Push(invocation);
        return invocation;
    }

    /// <summary>Reject a stale or foreign frame before its continuation can read or mutate game state.</summary>
    public void RequireContinuation(PhaseStep continuation)
    {
        if (agenda.Current is not { } current || !current.SameFrameAs(continuation))
            throw new InvalidOperationException("the continuation is not this game's current frame");
        if (ContinuationExecution.Of(continuation) is { } invocation)
        {
            RequireOwned(invocation);
            if (current.ContinuationPermit is not { Consumed: false } permit
                || permit.Execution != invocation)
                throw new InvalidOperationException("the continuation has already been consumed");
        }
    }

    /// <summary>Resume only the current, unconsumed continuation issued by this agenda.</summary>
    public void Resume(AbilityExecution invocation)
    {
        RequireOwned(invocation);
        var current = agenda.Current;
        var permit = current?.ContinuationPermit;
        if (permit is null || permit.Execution != invocation || permit.Consumed
            || permit.Frame != current!.Value.FrameId)
            throw new InvalidOperationException("the invocation has no current unused continuation");
        if (permit.Dependencies.Any(invocation.Frames.Contains)
            || invocation.Children.Any(child => !child.IsComplete && !child.DeferredActivation))
            throw new InvalidOperationException($"the continuation has unfinished child work: {string.Join(", ", agenda.items.Where(item => permit.Dependencies.Contains(item.Step.FrameId)).Select(item => item.Step.What + "#" + item.Step.FrameId))}");
        permit.Consumed = true;
        invocation.Frames.Remove(permit.Frame);
        invocation.SegmentOwnerFrame = current.Value.FrameId;
        invocation.SegmentFrames.Clear();
        invocation.State = AbilityExecutionState.Running;
        invocation.DeferredActivation = false;
        running.Push(invocation);
    }

    /// <summary>Yield an invocation whose continuation was registered before suspension.</summary>
    public void Suspend(AbilityExecution invocation)
    {
        RequireActive(invocation);
        if (invocation.Frames.Count == 0)
            throw new InvalidOperationException("suspension requires registered owned work");
        invocation.State = AbilityExecutionState.Suspended;
        running.Pop();
        PositionOwnedWork(invocation);
    }

    /// <summary>Finish the body; owned procedures must still finish before its window advances.</summary>
    public void Complete(AbilityExecution invocation)
    {
        RequireActive(invocation);
        invocation.State = AbilityExecutionState.Completing;
        running.Pop();
        PositionOwnedWork(invocation);
        CompleteOwnedInvocation(invocation);
    }

    /// <summary>Keep activation completion pending while its enclosing activation finishes.</summary>
    public void DeferActivation(AbilityExecution invocation)
    {
        RequireOwned(invocation);
        invocation.DeferredActivation = true;
    }

    internal PhaseStep Register(PhaseStep step, AbilityExecution? continuation)
    {
        var owner = continuation ?? Active ?? procedureOwner;
        if (owner is null) return step;
        RequireOwned(owner);
        long[] dependencies = continuation is null ? [] : [.. AbilityOwnedWork.Segment(owner)];
        owner.Frames.Add(step.FrameId);
        owner.SegmentFrames.Add(step.FrameId);
        return step with
        {
            ExecutionOwner = owner,
            ContinuationPermit = continuation is null ? null : new(owner, step.FrameId, dependencies),
        };
    }

    private void PositionOwnedWork(AbilityExecution invocation)
    {
        if (invocation.DeferredActivation) return;
        var owned = AbilityOwnedWork.Segment(invocation);
        var children = agenda.items.Where(item => owned.Contains(item.Step.FrameId)).ToList();
        if (children.Count == 0) return;
        int first = agenda.items.FindIndex(item => owned.Contains(item.Step.FrameId));
        agenda.items.RemoveAll(item => owned.Contains(item.Step.FrameId));
        int owner = agenda.items.FindIndex(item => item.Step.FrameId == invocation.SegmentOwnerFrame);
        agenda.items.InsertRange(owner >= 0 ? owner : Math.Min(first, agenda.items.Count),
            AbilityWorkOrdering.ByDependencies(children, this));
    }

    internal HashSet<long> WindowPrerequisites(Occurrence occurrence)
    {
        var frames = new HashSet<long>();
        foreach (var invocation in invocations.Where(invocation =>
            ReferenceEquals(invocation.Occurrence, occurrence) && invocation.WaitsNow))
            frames.UnionWith(AbilityOwnedWork.All(invocation));
        return frames;
    }

    internal bool Blocks(Occurrence occurrence) => invocations.Any(invocation =>
        ReferenceEquals(invocation.Occurrence, occurrence) && invocation.WaitsNow);

    internal void RequireWindowComplete(Occurrence occurrence)
    {
        if (Blocks(occurrence))
            throw new InvalidOperationException("the window owns an unfinished ability invocation");
    }

    internal bool OwnsUnfinishedWork(PhaseStep step) =>
        invocations.Any(invocation => invocation.OwnerFrame == step.FrameId
            && invocation.WaitsNow);

    internal void RequireAdvance(PhaseStep step)
    {
        if (OwnsUnfinishedWork(step))
            throw new InvalidOperationException("the agenda frame owns unfinished ability resolution");
    }

    internal void Removed(PhaseStep step, bool cancelled = false)
    {
        if (step.ExecutionOwner is { } owner) RequireOwned(owner);
        if (step.ContinuationPermit is { Consumed: false })
            throw new InvalidOperationException("an unconsumed continuation cannot be discarded");
        if (step.ExecutionOwner is { } execution)
        {
            execution.Frames.Remove(step.FrameId);
            CompleteOwnedInvocation(execution);
        }
    }

    private void CompleteOwnedInvocation(AbilityExecution invocation)
    {
        if (!invocation.ReadyToComplete) return;
        invocation.State = AbilityExecutionState.Complete;
        if (invocation.Resolution is { } ability) invocation.Occurrence.Resolution.Complete(ability);
        invocations.Remove(invocation);
        if (invocation.Parent is { } parent)
        {
            parent.Children.Remove(invocation);
            CompleteOwnedInvocation(parent);
        }
    }

    internal void RequireOwned(AbilityExecution invocation)
    {
        if (!ReferenceEquals(invocation.Agenda, agenda) || invocation.State == AbilityExecutionState.Abandoned)
            throw new InvalidOperationException("the invocation does not belong to this active game");
    }

    /// <summary>Validate the live invocation before a resolver reads or changes this game.</summary>
    public void RequireActive(AbilityExecution invocation)
    {
        RequireOwned(invocation);
        if (Active != invocation || invocation.State != AbilityExecutionState.Running)
            throw new InvalidOperationException("only the running invocation may change its execution state");
    }

    internal void Abandon()
    {
        foreach (var invocation in invocations) invocation.State = AbilityExecutionState.Abandoned;
        running.Clear();
        invocations.Clear();
    }
}
