using System.Security.Cryptography;
using Marvel.Rules.Play;
using Marvel.Session;
using Marvel.View;

namespace Marvel.Server;

/// <summary>A live game and the durable trace that reconstructs it.</summary>
internal sealed class HostedSession(string gameId, Game game, SessionSave save)
{
    public string GameId { get; } = gameId;

    public Game Game { get; private set; } = game;

    public SessionSave Save { get; private set; } = save;

    public long Revision => Save.Revision;

    public void Publish(Game candidate, SessionSave proposed)
    {
        Game = candidate;
        Save = proposed;
    }

    public void Publish(SessionSave proposed) => Save = proposed;
}
