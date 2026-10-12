using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Retains accepted local chronology; synchronization never appends a second copy.</summary>
internal sealed class TimelineArchive
{
    private readonly List<TimelineOccurrence> entries = [];
    private string? game;
    private long revision = -1;
    private readonly Dictionary<int, string> actions = [];
    internal IReadOnlyList<TimelineOccurrence> Entries => entries;

    internal void Clear()
    {
        entries.Clear();
        game = null;
        revision = -1;
        actions.Clear();
    }

    internal void Record(EngineResponse response, IReadOnlyList<EventPresentation> events, bool reset)
    {
        if (game != response.GameId)
        {
            entries.Clear();
            revision = -1;
            actions.Clear();
            game = response.GameId;
        }
        if (revision == response.Revision) return;
        revision = response.Revision;
        int cursor = response.History?.Cursor ?? 0;
        if (reset)
        {
            cursor = ReconciledCursor(response.History, cursor);
            entries.RemoveAll(entry => entry.Cursor > cursor);
            RestoreSummaries(response.History);
        }
        if (!reset || response.History?.Cursor is null or 0)
            entries.AddRange(events.Select(entry => new TimelineOccurrence(cursor, entry)));
        RememberActions(response.History);
    }

    private int ReconciledCursor(HistoryDescriptor? history, int cursor)
    {
        foreach (HistoryEntryDescriptor entry in history?.Entries ?? [])
            if (actions.TryGetValue(entry.Cursor, out string? identity) && identity != Identity(entry))
                cursor = Math.Min(cursor, entry.Cursor);
        return cursor;
    }

    private void RememberActions(HistoryDescriptor? history)
    {
        actions.Clear();
        foreach (HistoryEntryDescriptor entry in history?.Entries ?? []) actions[entry.Cursor] = Identity(entry);
    }

    private static string Identity(HistoryEntryDescriptor entry) => entry.Summary + "\n" + string.Join("\n", entry.Details);

    private void RestoreSummaries(HistoryDescriptor? history)
    {
        IReadOnlyList<HistoryEntryDescriptor> summaries = history?.Entries ?? [];
        int recorded = entries.Count == 0 ? -1 : entries[^1].Cursor;
        for (int index = 0; index < summaries.Count; index++)
        {
            HistoryEntryDescriptor entry = summaries[index];
            int end = index + 1 < summaries.Count ? summaries[index + 1].Cursor : history!.Cursor;
            if (end <= recorded) continue;
            entries.Add(new TimelineOccurrence(end,
                new EventPresentation(entry.Summary, "Earlier action summary", [], EventMotionKind.State)));
        }
    }
}
