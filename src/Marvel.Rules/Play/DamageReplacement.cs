using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>The game-owned step-one damage amount, writable only by its active replacement abilities.</summary>
public sealed class DamageReplacement
{
    internal DamageReplacement(DamageRequest request, AbilityExecution execution)
    {
        Source = request.Source;
        Target = request.Target;
        TargetIncarnation = Target.Incarnation;
        CurrentAmount = Math.Max(0, request.Amount);
        IgnoresTough = request.IgnoresTough;
        Execution = execution;
        execution.Occurrence.Damage = this;
    }

    /// <summary>The card dealing the damage.</summary>
    public Card Source { get; }
    /// <summary>The physical recipient retained through replacement choices.</summary>
    public Card Target { get; }
    /// <summary>The recipient's in-play identity when damage began.</summary>
    public int TargetIncarnation { get; }
    /// <summary>Whether this damage is placed past a tough status card.</summary>
    public bool IgnoresTough { get; }
    /// <summary>The invocation owning all replacement work.</summary>
    public AbilityExecution Execution { get; }
    /// <summary>The identity whose attack would deal this damage beyond the target's remaining hit points, if any.</summary>
    public Timing.OccurrenceCard? ExcessDealer { get; init; }
    /// <summary>The amount available to the currently executing replacement ability.</summary>
    public long CurrentAmount { get; private set; }
    /// <summary>Whether every replacement ability and its children have completed.</summary>
    public bool IsComplete => Execution.IsComplete;
    /// <summary>The fixed dealt amount; unavailable while replacement work remains.</summary>
    public long ResolvedAmount => IsComplete ? CurrentAmount
        : throw new InvalidOperationException("damage replacement has not completed");

    /// <summary>Replace the imminent damage from an active ability in this replacement batch.</summary>
    public void Replace(AbilityExecution invocation, long amount)
    {
        Execution.Agenda.Executions.RequireActive(invocation);
        var owner = invocation;
        while (owner is not null && !ReferenceEquals(owner, Execution)) owner = owner.Parent;
        if (owner is null) throw new InvalidOperationException("the ability does not own this damage replacement");
        CurrentAmount = Math.Max(0, amount);
    }
}
