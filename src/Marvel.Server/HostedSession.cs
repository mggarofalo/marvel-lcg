using System.Security.Cryptography;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.View;

namespace Marvel.Server;

/// <summary>A live game and the durable trace that reconstructs it.</summary>
internal sealed class HostedSession(string gameId, Game game, SessionSave save)
{
    public object Gate { get; } = new();

    public string GameId { get; } = gameId;

    public Game Game { get; private set; } = game;

    public SessionSave Save { get; private set; } = save;

    public IReadOnlyList<HistoryUnitInspection>? History { get; set; }

    public long Revision => Save.Revision;

    public void Publish(Game candidate, SessionSave proposed, IReadOnlyList<HistoryUnitInspection> history)
    {
        History = history;
        Game = candidate;
        Save = proposed;
    }

    public void Publish(SessionSave proposed) => Save = proposed;
}
