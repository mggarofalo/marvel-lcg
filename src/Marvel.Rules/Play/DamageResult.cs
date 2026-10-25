namespace Marvel.Rules.Play;

/// <summary>One damage execution and its final dealt/taken facts, retained after the recipient leaves play.</summary>
public sealed class DamageResult
{
    private Damage.PlacedDamage? placed;
    private Damage.Outcome outcome;

    internal DamageResult(AbilityExecution execution) => Execution = execution;
    /// <summary>The invocation owning replacement, placement, and defeat work.</summary>
    public AbilityExecution Execution { get; }
    /// <summary>Whether all damage work has completed.</summary>
    public bool IsComplete => Execution.IsComplete;
    /// <summary>Whether placement has fixed the dealt and taken amounts.</summary>
    public bool IsPlaced => placed is not null;
    /// <summary>The completed defeat outcome, or an explicit suspension.</summary>
    public Damage.Outcome Outcome => IsComplete ? outcome : Damage.Outcome.Suspended;
    /// <summary>Damage dealt after replacement.</summary>
    public long Dealt => Placement.Dealt;
    /// <summary>Damage actually placed after prevention.</summary>
    public long Taken => Placement.Taken;
    private Damage.PlacedDamage Placement => placed
        ?? throw new InvalidOperationException("damage amounts are unresolved before placement");

    /// <summary>The exact physical recipient and incarnation at damage initiation.</summary>
    public int Target { get; internal init; }
    /// <summary>The recipient incarnation, retained across defeat and re-entry.</summary>
    public int TargetIncarnation { get; internal init; }

    internal void Place(Damage.PlacedDamage value) => placed = value;
    internal void Complete(Damage.Outcome value) => outcome = value;
}
