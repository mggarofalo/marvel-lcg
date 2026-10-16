using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Preserves scheduling order while placing every prerequisite before its continuation.</summary>
internal static class AbilityWorkOrdering
{
    internal static IReadOnlyList<(PhaseStep Step, Stage Stage, Occurrence? Occurrence)> ByDependencies(
        List<(PhaseStep Step, Stage Stage, Occurrence? Occurrence)> remaining, AbilityExecutions executions)
    {
        var ordered = new List<(PhaseStep Step, Stage Stage, Occurrence? Occurrence)>();
        var visiting = new HashSet<long>();
        var dependencies = remaining.ToDictionary(item => item.Step.FrameId,
            item => Prerequisites(item, executions));
        while (remaining.Count > 0) Append(remaining[0], remaining, ordered, visiting, dependencies);
        return ordered;
    }

    private static void Append(
        (PhaseStep Step, Stage Stage, Occurrence? Occurrence) item,
        List<(PhaseStep Step, Stage Stage, Occurrence? Occurrence)> remaining,
        List<(PhaseStep Step, Stage Stage, Occurrence? Occurrence)> ordered,
        HashSet<long> visiting, IReadOnlyDictionary<long, HashSet<long>> dependencies)
    {
        if (!visiting.Add(item.Step.FrameId))
            throw new InvalidOperationException("ability work contains a dependency cycle");
        foreach (var dependency in remaining.Where(candidate => dependencies[item.Step.FrameId].Contains(candidate.Step.FrameId)).ToArray())
            if (remaining.Contains(dependency)) Append(dependency, remaining, ordered, visiting, dependencies);
        remaining.Remove(item);
        visiting.Remove(item.Step.FrameId);
        ordered.Add(item);
    }
    private static HashSet<long> Prerequisites(
        (PhaseStep Step, Stage Stage, Occurrence? Occurrence) item, AbilityExecutions executions)
    {
        var frames = new HashSet<long>();
        if (item.Step.ContinuationPermit is { Consumed: false } permit)
            frames.UnionWith(permit.Dependencies);
        if (item.Step.CompletionPrerequisite is { } prerequisite)
            frames.UnionWith(AbilityOwnedWork.All(prerequisite));
        if (!item.Step.Plan && item.Stage is Stage.Interrupts or Stage.Responses
            && item.Occurrence is { } occurrence)
            frames.UnionWith(executions.WindowPrerequisites(occurrence));
        return frames;
    }
}
