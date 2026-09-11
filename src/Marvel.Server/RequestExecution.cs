using System.Diagnostics;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Request-local mutation outcome and operational observation.</summary>
internal sealed class RequestExecution
{
    private readonly OperationalLog log;
    private readonly EngineRequest request;
    private readonly int? authorizedSeat;
    private readonly long? priorRevision;
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private bool replayDiverged;
    private bool sessionRetired;

    public RequestExecution(
        OperationalLog log,
        EngineRequest request,
        int? authorizedSeat,
        long? priorRevision)
    {
        this.log = log;
        this.request = request;
        this.authorizedSeat = authorizedSeat;
        this.priorRevision = priorRevision;
    }

    public T ObserveReplay<T>(Func<T> work)
    {
        var replayElapsed = Stopwatch.StartNew();
        try
        {
            T result = work();
            replayElapsed.Stop();
            log.Write(
                OperationalEventIds.ReplayCompleted, "accepted",
                replayElapsed.ElapsedMilliseconds, request.RequestId, request.GameId,
                operation: "replay", replayVerified: true);
            return result;
        }
        catch (Exception failure)
        {
            replayElapsed.Stop();
            replayDiverged |= failure is ReplayDivergenceException;
            log.Write(
                OperationalEventIds.ReplayCompleted, "rejected",
                replayElapsed.ElapsedMilliseconds, request.RequestId, request.GameId,
                operation: "replay",
                replayDiverged: failure is ReplayDivergenceException,
                errorCode: failure is ReplayDivergenceException
                    ? "replay_diverged"
                    : "replay_failed");
            throw;
        }
    }

    public string? ObservePersistence(Func<string?> work)
    {
        var persistenceElapsed = Stopwatch.StartNew();
        try
        {
            string? saveGeneration = work();
            persistenceElapsed.Stop();
            log.Write(
                OperationalEventIds.PersistenceCompleted, "accepted",
                persistenceElapsed.ElapsedMilliseconds, request.RequestId, request.GameId,
                operation: "persistence", saveCommitted: true,
                expectedRevision: request.ExpectedRevision,
                saveGeneration: saveGeneration);
            return saveGeneration;
        }
        catch (Exception failure)
        {
            persistenceElapsed.Stop();
            log.Write(
                OperationalEventIds.PersistenceCompleted, "rejected",
                persistenceElapsed.ElapsedMilliseconds, request.RequestId, request.GameId,
                operation: "persistence", errorCode: "persistence_failed",
                expectedRevision: request.ExpectedRevision);
            throw new PersistenceFailureException(failure);
        }
    }

    public void MarkReplayDiverged() => replayDiverged = true;

    public void MarkSessionRetired() => sessionRetired = true;

    public EngineResponse Complete(EngineResponse response)
    {
        elapsed.Stop();
        bool accepted = response.Error is null;
        bool mutates = request.Operation is EngineProtocol.Open
            or EngineProtocol.Attach
            or EngineProtocol.Resolve
            or EngineProtocol.Undo
            or EngineProtocol.Redo
            or EngineProtocol.Reorder
            or EngineProtocol.Close;
        bool replays = request.Operation is EngineProtocol.Resolve
            or EngineProtocol.Undo
            or EngineProtocol.Redo
            or EngineProtocol.Reorder;
        string disposition = accepted
            ? "accepted"
            : response.Error!.Code.StartsWith("stale_", StringComparison.Ordinal)
                ? "stale"
                : "rejected";
        long? observedRevision = request.Operation switch
        {
            EngineProtocol.Setup => null,
            EngineProtocol.Close => priorRevision,
            _ when accepted => response.Revision,
            _ => priorRevision,
        };
        log.Write(
            OperationalEventIds.RequestCompleted,
            disposition,
            elapsed.ElapsedMilliseconds,
            request.RequestId,
            request.GameId,
            request.Operation,
            observedRevision,
            authorizedSeat,
            saveCommitted: accepted && mutates,
            replayVerified: accepted && replays,
            replayDiverged: replayDiverged,
            sessionRetired: sessionRetired,
            errorCode: response.Error?.Code,
            expectedRevision: request.ExpectedRevision);
        return response;
    }
}

internal sealed class PersistenceFailureException(Exception failure)
    : IOException("session persistence failed", failure);
