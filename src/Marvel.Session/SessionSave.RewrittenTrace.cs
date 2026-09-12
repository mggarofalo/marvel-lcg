using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>

/// <summary>A newly derived active trace and the game it produces.</summary>
public sealed record RewrittenTrace(
    Game Game,
    IReadOnlyList<JournalUnit> Units,
    int EditFrontier);
