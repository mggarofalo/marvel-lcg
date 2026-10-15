using Marvel.Rules.Play;

namespace Marvel.View;

/// <summary>Groups one accepted player commitment, keeping semantic phase and ability boundaries explicit.</summary>
public static class AcceptedActivityOccurrence
{
    /// <summary>Costs and consequences stay ordered beneath the accepted intention; phase procedures retain their own rows.</summary>
    public static IReadOnlyList<EventPresentation> From(IReadOnlyList<EventPresentation> entries,
        DecisionReceiptContext? accepted)
    {
        if (accepted is null || !CanGroup(entries, accepted)) return entries;
        EventPresentation result = entries.LastOrDefault(entry => entry.Motion is
            EventMotionKind.Damage or EventMotionKind.Threat or EventMotionKind.Heal or EventMotionKind.Defeat)
            ?? entries[^1];
        string summary = accepted.Commitment.TrimEnd('.') + " · " + (result.CueSummary ?? result.Summary);
        return [new EventPresentation(summary, accepted.Commitment, accepted.Anchors, result.Motion)
        {
            CueSummary = accepted.OccurrenceTitle ?? accepted.Commitment.TrimEnd('.'),
            ResultSummary = result.CueSummary ?? result.Summary,
            Activity = result.Activity,
            Details = entries.ToArray(),
        }];
    }

    private static bool CanGroup(IReadOnlyList<EventPresentation> entries, DecisionReceiptContext accepted) =>
        accepted.ActionVerb is not (null or Game.EndPhaseVerb) && entries.Count >= 2 && !entries.Any(IsBoundary);

    private static bool IsBoundary(EventPresentation entry) => entry.Activity is
        { PhaseBoundary: true } or { PhaseStep: true } or { Batch: not null }
        or { AttackIdentity: not null } or { AttackBoundary: not null }
        or { Operation: "Ability_Resolved" or "Ability_Initiated" } || entry.Motion == EventMotionKind.Terminal;
}
