using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>Visibility-safe history boundaries currently editable by this capability.</summary>
public sealed record HistoryDescriptor(
    int Cursor,
    IReadOnlyList<int> Undo,
    IReadOnlyList<int> Redo,
    IReadOnlyList<HistoryEntryDescriptor> Entries,
    bool ActionOpen);
