using System.Globalization;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Compacts explicit elimination and terminal cleanup while preserving every authorized receipt.</summary>
internal static class EventActivityBeats
{
    internal static IReadOnlyList<EventPresentation> From(IReadOnlyList<EventPresentation> entries)
    {
        if (entries.Count == 0) return entries;
        int terminalSuffix = TerminalSuffix(entries);
        List<EventPresentation> beats = [];
        for (int index = 0; index < entries.Count;)
        {
            if (!Cleanup(entries[index], index >= terminalSuffix))
            {
                beats.Add(entries[index++]);
                continue;
            }
            int first = index++;
            while (index < entries.Count && Cleanup(entries[index], index >= terminalSuffix)) index++;
            EventPresentation[] run = [.. entries.Skip(first).Take(index - first)];
            beats.Add(run.Length > 1 ? Combine(run) : run[0]);
        }
        return beats;
    }

    private static int TerminalSuffix(IReadOnlyList<EventPresentation> entries)
    {
        if (entries[^1].Motion != EventMotionKind.Terminal) return entries.Count;
        int first = entries.Count - 1;
        while (first > 0 && Movement(entries[first - 1])) first--;
        return first;
    }

    // The engine names elimination explicitly. Other movements receive this treatment only
    // in the terminal suffix; costs, reveals and later consequences keep their own beats.
    private static bool Cleanup(EventPresentation entry, bool terminalSuffix) => Movement(entry)
        && (terminalSuffix || entry.Activity!.Operation == "Eliminate");

    private static bool Movement(EventPresentation entry) => entry.Motion == EventMotionKind.Move
        && entry.Details.Count == 0 && entry.ForcedAbilityNotification is null
        && entry.Activity is { StatusChange: ActivityStatusChange.None, Quantities.Count: 0,
            PhaseBoundary: false, PhaseStep: false, Batch: null }
        && (entry.Activity.Operation is "Move" or "Remove" or "Discard" or "Detach" or "Eliminate"
            || entry.Activity.Operation == "" && entry.Activity.From.Length > 0 && entry.Activity.To.Length > 0);

    private static EventPresentation Combine(IReadOnlyList<EventPresentation> movements)
    {
        ActivitySubject[] subjects = movements.SelectMany(entry => entry.Activity!.Subjects)
            .DistinctBy(subject => subject.Card).ToArray();
        string count = subjects.Length.ToString(CultureInfo.InvariantCulture);
        bool elimination = movements.All(entry => entry.Activity!.Operation == "Eliminate");
        string cards = subjects.Length == 1 ? "card" : "cards";
        string summary = elimination ? $"Player elimination · {count} {cards} removed"
            : $"Table cleanup · {count} {cards} moved";
        // This is one visual cleanup beat, not a claim that the engine moved cards simultaneously.
        string kind = elimination ? "Player elimination" : "Accepted game outcome";
        return new(summary, kind, subjects.Select(subject => subject.Card).ToArray(), EventMotionKind.Move)
        {
            Activity = new(subjects, []),
            Details = movements.ToArray(),
        };
    }
}
