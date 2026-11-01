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

    /// <summary>The amount an attack-wide prevention removed at step 1; still dealt, never taken.</summary>
    /// <remarks>
    /// rr:prevent.1.1: "the amount of damage that character takes is reduced, but the
    /// amount of damage dealt is not reduced", and rr:prevent.1.3: "If all damage from
    /// an attack is prevented, the attacking character is considered to have dealt
    /// damage". Rulings 70423690d446327f and 05670207b5acc4d9 measure excess damage on
    /// that dealt amount.
    /// </remarks>
    public long PreventedAmount { get; private set; }

    /// <summary>Whether an attack-wide prevention removes all of this damage once step 1 finishes.</summary>
    /// <remarks>
    /// The prevention was installed when the attack began (Temporal Shield, 11014), so
    /// no forced step-1 replacement sees the damage. An optional interrupt to the excess
    /// the attack deals (Follow Through, 16045) still does: rr:prevent.1.3 has the
    /// attacker deal that damage, and the increased amount is prevented with the rest.
    /// </remarks>
    public bool PreventsAll { get; internal init; }

    /// <summary>The step-1 request continuing to placement with the fixed dealt and prevented amounts.</summary>
    internal DamageRequest Resolve(DamageRequest request) =>
        request with { Amount = ResolvedAmount, PreventedAtStepOne = PreventedAmount };

    /// <summary>Prevent all the imminent damage from an active ability in this replacement batch.</summary>
    public void PreventAll(AbilityExecution invocation)
    {
        long amount = CurrentAmount;
        Replace(invocation, 0);
        PreventedAmount += amount;
    }

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
