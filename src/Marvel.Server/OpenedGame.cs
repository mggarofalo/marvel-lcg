using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>A game and the card-text events produced during its setup.</summary>
public sealed record OpenedGame(Game Game, IReadOnlyList<GameEvent> SetupEvents);
