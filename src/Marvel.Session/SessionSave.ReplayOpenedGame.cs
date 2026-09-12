using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>

/// <summary>The game and setup events freshly produced by a replay factory.</summary>
public sealed record ReplayOpenedGame(Game Game, IReadOnlyList<GameEvent> SetupEvents);
