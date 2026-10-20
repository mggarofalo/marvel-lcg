namespace Marvel.Rules.Play;

/// <summary>Completion and per-recipient evidence for simultaneous attack damage.</summary>
public sealed class SimultaneousAttackDamageResult
{
    private IReadOnlyList<AttackDamageResult> recipients = [];

    internal SimultaneousAttackDamageResult(AbilityExecution execution) => Execution = execution;
    /// <summary>The execution owning every recipient and its aftermath.</summary>
    public AbilityExecution Execution { get; }
    /// <summary>Whether replacements, placements, defeats and aftermath have finished.</summary>
    public bool IsComplete => Execution.IsComplete;
    /// <summary>Completed damage results in the caller's declared target order.</summary>
    public IReadOnlyList<AttackDamageResult> Recipients => IsComplete
        ? recipients
        : throw new InvalidOperationException("simultaneous attack damage is still resolving");

    internal void Publish(IEnumerable<AttackDamageResult> results) => recipients = Array.AsReadOnly(results.ToArray());
}
