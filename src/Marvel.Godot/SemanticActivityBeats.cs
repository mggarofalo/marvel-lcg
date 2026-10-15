using Marvel.View;

namespace Marvel.Godot;

/// <summary>Folds a card's supporting travel into its reveal, boost or installation.</summary>
internal static class SemanticActivityBeats
{
    internal static IReadOnlyList<EventPresentation> From(IReadOnlyList<EventPresentation> entries)
    {
        List<EventPresentation> beats = [];
        foreach (EventPresentation original in entries)
        {
            if (original.Activity?.Kind == EventActivityKind.Topology) continue;
            EventPresentation entry = !original.Details.Any(HasTopology) ? original : original with
                { Details = WithoutTopology(original.Details) };
            if (beats.Count > 0 && CanCombine(beats[^1], entry))
                beats[^1] = Combine(beats[^1], entry);
            else beats.Add(entry);
        }
        return beats;
    }

    private static bool HasTopology(EventPresentation entry) => entry.Activity?.Kind == EventActivityKind.Topology
        || entry.Details.Any(HasTopology);

    private static EventPresentation[] WithoutTopology(IReadOnlyList<EventPresentation> entries) =>
        entries.Where(entry => entry.Activity?.Kind != EventActivityKind.Topology)
            .Select(entry => entry.Details.Count == 0 ? entry : entry with { Details = WithoutTopology(entry.Details) }).ToArray();

    private static bool CanCombine(EventPresentation first, EventPresentation next)
    {
        if (IsBoundary(first) || IsBoundary(next)) return false;
        var left = first.Activity!;
        var right = next.Activity!;
        return left.AttackIdentity == right.AttackIdentity && SharesSubject(left, right)
            && SupportingTransition(left.Kind, right.Kind);
    }

    private static bool IsBoundary(EventPresentation entry) => entry.ForcedAbilityNotification is not null
        || entry.Activity is not { } activity || activity.AttackBoundary is not null
        || activity.PhaseBoundary || activity.PhaseStep;

    private static bool SharesSubject(EventActivityDescriptor left, EventActivityDescriptor right) =>
        left.Subjects.Where(IsMovingSubject).Any(subject =>
            right.Subjects.Where(IsMovingSubject).Any(other => other.Card == subject.Card));

    private static bool IsMovingSubject(ActivitySubject subject) =>
        subject.Role is ActivitySubjectRole.Subject or ActivitySubjectRole.Boost;

    private static bool SupportingTransition(EventActivityKind left, EventActivityKind right) => left switch
    {
        EventActivityKind.Travel => right is EventActivityKind.Travel or EventActivityKind.Reveal
            or EventActivityKind.Boost or EventActivityKind.Installation,
        EventActivityKind.Reveal => right is EventActivityKind.Travel or EventActivityKind.Boost or EventActivityKind.Installation,
        EventActivityKind.Boost or EventActivityKind.Installation => right == EventActivityKind.Travel,
        _ => false,
    };

    private static EventPresentation Combine(EventPresentation first, EventPresentation next)
    {
        EventPresentation headline = next.Activity!.Kind == EventActivityKind.Travel ? first : next;
        return headline with
        {
            Details = [.. Parts(first), .. Parts(next)],
            Anchors = first.Anchors.Concat(next.Anchors).Distinct().ToArray(),
        };
    }

    private static IEnumerable<EventPresentation> Parts(EventPresentation entry) =>
        entry.Details.Count == 0 ? [entry] : entry.Details;
}
