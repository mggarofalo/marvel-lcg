using Marvel.Rules.Events;
using Marvel.Rules.Play;

namespace Marvel.Sim;

internal sealed record OpenedGame(
    Game Game,
    IReadOnlyList<GameEvent> SetupEvents);
