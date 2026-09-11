using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>
/// One fully verified candidate mutation that may replace a live hosted game.
/// </summary>
/// <remarks>
/// Response projection happens before persistence, then the complete durable
/// bundle commits before the live game is published. A failure at either
/// earlier boundary therefore leaves the live session unchanged.
/// </remarks>
internal sealed record SessionTransaction(Game Candidate, SessionSave Proposed)
{
    public EngineResponse CommitAndPublish(
        HostedSession live,
        IReadOnlyList<StoredAuthority> authorities,
        Func<StoredSession, string?> commit,
        Func<Game, SessionSave, EngineResponse> project)
    {
        ArgumentNullException.ThrowIfNull(live);
        ArgumentNullException.ThrowIfNull(authorities);
        ArgumentNullException.ThrowIfNull(commit);
        ArgumentNullException.ThrowIfNull(project);

        EngineResponse response = project(Candidate, Proposed);
        _ = commit(new StoredSession(Proposed, authorities));
        live.Publish(Candidate, Proposed);
        return response;
    }
}
