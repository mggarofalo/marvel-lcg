using Marvel.Observability;
using System.Diagnostics;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Restores and publishes one stored session, or refuses it without touching its files.</summary>
internal sealed class SessionRestoreAttempt(
    EngineHost host, string? generation, StoredSession stored)
{
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private HostedSession? restoring;

    internal void Restore()
    {
        try { RestoreVerified(); }
        catch (Exception failure) { Reject(failure); }
    }

    private void RestoreVerified()
    {
        Game game = SessionReplay.Verify(stored.Save, host.compatibility, host.ReplayOpen);
        host.authority.ValidateForRestore(stored, game);
        restoring = host.authority.PublishRestored(stored, game);
        elapsed.Stop();
        host.log.Write(OperationalEventIds.SessionRestored, "accepted",
            elapsed.ElapsedMilliseconds, gameId: stored.Save.Session.Label,
            revision: stored.Save.Revision, saveCommitted: false,
            replayVerified: true, saveGeneration: generation,
            stage: "restore");
    }

    private void Reject(Exception failure)
    {
        if (restoring is not null) host.authority.Remove(restoring);
        if (failure is SessionCompatibilityException incompatible) host.refused.Refuse(stored, incompatible);
        elapsed.Stop();
        host.log.Write(OperationalEventIds.SessionRestoreFailed, "rejected",
            elapsed.ElapsedMilliseconds, gameId: stored.Save.Session.Label,
            revision: stored.Save.Revision, saveCommitted: false,
            replayDiverged: failure is ReplayDivergenceException,
            saveGeneration: generation, stage: "quarantine",
            errorCode: ErrorCode(failure));
    }

    private static string ErrorCode(Exception failure) => failure switch
    {
        ReplayDivergenceException => "replay_diverged",
        SessionCompatibilityException mismatch => mismatch.Category,
        _ => "restore_failed",
    };
}
