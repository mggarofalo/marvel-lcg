namespace Marvel.Rules.Play;

/// <summary>Completion and retained per-recipient facts for simultaneous direct damage.</summary>
public sealed class SimultaneousDirectDamageResult
{
    private IReadOnlyList<DamageResult> recipients = [];
    internal SimultaneousDirectDamageResult(AbilityExecution execution) => Execution = execution;
    /// <summary>The invocation owning replacements, placements and defeats.</summary>
    public AbilityExecution Execution { get; }
    /// <summary>Whether every recipient has completed its damage procedure.</summary>
    public bool IsComplete => Execution.IsComplete;
    /// <summary>Final damage facts in the caller's declared recipient order.</summary>
    public IReadOnlyList<DamageResult> Recipients => IsComplete ? recipients
        : throw new InvalidOperationException("simultaneous direct damage is still resolving");
    internal void Publish(IEnumerable<DamageResult> results) => recipients = Array.AsReadOnly(results.ToArray());
}
