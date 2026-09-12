using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>One completed active action and the cursor immediately before it.</summary>
public sealed record HistoryEntryDescriptor(
    int Cursor,
    string Summary,
    IReadOnlyList<string> Details);
