using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

internal static class DecisionTargetProgress
{
    internal static TargetSelectionProgress Compute(
        Affordance? selected, IReadOnlyList<int> targets)
    {
        TargetRequest? request = selected?.Targets;
        if (request is null)
        {
            return new TargetSelectionProgress(
                TargetSelectionMode.None,
                targets.Count,
                0,
                0,
                targets.Count == 0);
        }

        bool satisfied = request.Allows(targets);
        if (request.IsGrouped)
        {
            return new TargetSelectionProgress(
                TargetSelectionMode.Grouped,
                satisfied ? 1 : 0,
                1,
                1,
                satisfied);
        }

        return new TargetSelectionProgress(
            request.AllowRepeated
                ? TargetSelectionMode.Repeated
                : TargetSelectionMode.Ordinary,
            targets.Count,
            request.Min,
            request.Max,
            satisfied);
    }
}
