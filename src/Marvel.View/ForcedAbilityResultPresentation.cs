using Marvel.Rules.Events;
using Marvel.Rules.Timing;

namespace Marvel.View;

/// <summary>Accounts for actual authorized results explicitly associated by the engine with a forced ability.</summary>
internal static class ForcedAbilityResultPresentation
{
    internal static EventPresentation[] Attach(IReadOnlyList<GameEvent> events, EventPresentation[] presented)
    {
        var results = events.Select((happened, index) => (happened, index))
            .Where(item => item.happened.AbilitySource is not null && item.happened.AbilityOccurrence is not null)
            .GroupBy(item => (item.happened.AbilitySource!.Value, item.happened.AbilityOccurrence!.Value, item.happened.AbilityTiming, item.happened.AbilityOrdinal))
            .ToDictionary(group => group.Key, group => group.Select(item => presented[item.index]).ToArray());
        for (int index = 0; index < events.Count; index++)
        {
            if (events[index] is not AbilityWindowResult receipt
                || !results.TryGetValue((receipt.Source, receipt.Occurrence, receipt.Type, receipt.Ordinal), out EventPresentation[]? outcomes)) continue;
            EventPresentation entry = presented[index];
            string summary = string.Join(" ", outcomes.Select(outcome => outcome.CueSummary ?? outcome.Summary));
            if (!receipt.Completed) summary += " Resolution awaits a decision.";
            presented[index] = entry with
            {
                Summary = entry.ForcedAbilityNotification!.Title + ": " + summary,
                ForcedAbilityNotification = entry.ForcedAbilityNotification with { Summary = summary },
                Details = outcomes,
                Activity = entry.Activity! with
                {
                    Subjects = entry.Activity.Subjects.Concat(outcomes.SelectMany(outcome => outcome.Activity?.Subjects ?? []))
                        .DistinctBy(subject => (subject.Card, subject.Role)).ToArray(),
                },
            };
        }
        return presented;
    }

    internal static bool IsGroupedResult(GameEvent happened, IReadOnlySet<(int, int, AbilityType?, int?)> receipts) =>
        happened.AbilitySource is { } source && happened.AbilityOccurrence is { } occurrence
        && receipts.Contains((source, occurrence, happened.AbilityTiming, happened.AbilityOrdinal));
}
