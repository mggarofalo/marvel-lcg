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

    // A cache of the verified construction game for `Save`; see `ReplayCheckpoint`.
    private ReplayCheckpoint? checkpoint;

    public void Publish(
        Game candidate,
        SessionSave proposed,
        IReadOnlyList<HistoryUnitInspection> history,
        ReplayCheckpoint? next = null)
    {
        History = history;
        Game = candidate;
        Save = proposed;
        checkpoint = next;
    }

    public void Publish(SessionSave proposed) => Save = proposed;

    /// <summary>Removes the checkpoint so that only one decision constructs on it.</summary>
    public ReplayCheckpoint? TakeCheckpoint()
    {
        ReplayCheckpoint? taken = checkpoint;
        checkpoint = null;
        return taken;
    }

    /// <summary>Replaces a live game that a failed decision left unverified.</summary>
    public void Recover(Game verified) => Game = verified;
}
