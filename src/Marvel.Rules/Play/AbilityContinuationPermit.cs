namespace Marvel.Rules.Play;

/// <summary>One scheduler-issued right to resume an invocation at its registered frame.</summary>
internal sealed class AbilityContinuationPermit(AbilityExecution execution, long frame, long[] dependencies)
{
    internal AbilityExecution Execution { get; } = execution;
    internal long Frame { get; } = frame;
    internal long[] Dependencies { get; } = dependencies;
    internal bool Consumed { get; set; }

}
