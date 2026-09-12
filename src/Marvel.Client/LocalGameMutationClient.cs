using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Server;

using static Marvel.Client.ClientResponseValidation;

namespace Marvel.Client;

/// <summary>Owns mutation transport and uncertain-response recovery.</summary>
internal static class LocalGameMutationClient
{
    /// <summary>
    /// Submits one answer and, after a rejection or uncertain response, reads
    /// the current view without ever repeating the mutation.
    /// </summary>
    internal static async ValueTask<ClientResolutionResult> ResolveAsync(
        this LocalGameClient client, string capability,
        EngineDecision decision,
        CancellationToken cancellationToken = default)
    {
        return await client.ResolveAsync(
            new ClientSession(LocalGameSession.GameId, capability),
            decision,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Submits one answer for an explicit session and recovers that same
    /// session without ever repeating the mutation.
    /// </summary>
    internal static async ValueTask<ClientResolutionResult> ResolveAsync(
        this LocalGameClient client, ClientSession session,
        EngineDecision decision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(decision);
        ClientStartupError? sessionFailure = SessionError(session);
        if (sessionFailure is not null)
        {
            return new ClientResolutionResult(
                Response: null,
                sessionFailure,
                ClientMutationDisposition.NotSent,
                ClientSessionDisposition.Unavailable);
        }

        cancellationToken.ThrowIfCancellationRequested();
        ClientStartupError? failure = null;
        ClientMutationDisposition mutation = ClientMutationDisposition.Uncertain;
        long expectedRevision = client.RevisionFor(session);
        try
        {
            string requestId = client.NextRequestId(LocalGameClient.ResolveRequestId);
            EngineResponse response = await client.transport.ExchangeAsync(
                EngineRequest.ResolveGame(
                    requestId,
                    session.GameId,
                    session.Capability,
                    decision,
                    expectedRevision),
                cancellationToken).ConfigureAwait(false);
            var interpreted = InterpretMutationResponse(
                client, session, response, requestId, expectedRevision);
            if (interpreted.Result is not null) return interpreted.Result;
            failure = interpreted.Failure
                ?? throw new InvalidOperationException("mutation response has no disposition");
            mutation = interpreted.Mutation;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (EngineTransportException failureBeforeResponse)
            when (!failureBeforeResponse.RequestMayHaveCommitted)
        {
            return new ClientResolutionResult(
                Response: null,
                Error(
                    "transport_unavailable",
                    "The decision was not sent. Try it again when the game service is available."),
                ClientMutationDisposition.NotSent,
                ClientSessionDisposition.Active);
        }
        catch (Exception)
        {
            failure = Error(
                "transport_unavailable",
                "The decision response was lost. The client will read the current table without repeating it.");
        }

        return await client.RecoverCurrentViewAsync(session, failure, mutation)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Reconstructs the session at one server-advertised earlier history
    /// boundary without ever retrying an uncertain mutation.
    /// </summary>
    internal static async ValueTask<ClientResolutionResult> UndoAsync(
        this LocalGameClient client, ClientSession session,
        int cursor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ClientStartupError? sessionFailure = SessionError(session);
        if (sessionFailure is not null)
        {
            return new ClientResolutionResult(
                Response: null,
                sessionFailure,
                ClientMutationDisposition.NotSent,
                ClientSessionDisposition.Unavailable);
        }

        if (!HistoryAvailable(client, session, cursor))
        {
            return new ClientResolutionResult(
                Response: null,
                Error(
                    "history_unavailable",
                    "New information or table authority makes that history point unavailable."),
                ClientMutationDisposition.NotSent,
                ClientSessionDisposition.Active);
        }

        cancellationToken.ThrowIfCancellationRequested();
        ClientStartupError? failure = null;
        ClientMutationDisposition mutation = ClientMutationDisposition.Uncertain;
        long expectedRevision = client.RevisionFor(session);
        try
        {
            string requestId = client.NextRequestId(LocalGameClient.UndoRequestId);
            EngineResponse response = await client.transport.ExchangeAsync(
                EngineRequest.UndoGame(
                    requestId,
                    session.GameId,
                    session.Capability,
                    cursor,
                    expectedRevision),
                cancellationToken).ConfigureAwait(false);
            var interpreted = InterpretMutationResponse(
                client, session, response, requestId, expectedRevision);
            if (interpreted.Result is not null) return interpreted.Result;
            failure = interpreted.Failure
                ?? throw new InvalidOperationException("mutation response has no disposition");
            mutation = interpreted.Mutation;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (EngineTransportException failureBeforeResponse)
            when (!failureBeforeResponse.RequestMayHaveCommitted)
        {
            return new ClientResolutionResult(
                Response: null,
                Error(
                    "transport_unavailable",
                    "The history change was not sent. Try it again when the game service is available."),
                ClientMutationDisposition.NotSent,
                ClientSessionDisposition.Active);
        }
        catch (Exception)
        {
            failure = Error(
                "transport_unavailable",
                "The undo response was lost. The client will read the current table without repeating it.");
        }

        return await client.RecoverCurrentViewAsync(session, failure, mutation)
            .ConfigureAwait(false);
    }

    private static bool HistoryAvailable(
        LocalGameClient client, ClientSession session, int cursor) =>
        client.histories.TryGetValue(session, out HistoryDescriptor? history)
        && history.Undo.Contains(cursor);

    /// <summary>Reads one complete current view without mutating game state.</summary>
    private static MutationResponse InterpretMutationResponse(
        LocalGameClient client, ClientSession session, EngineResponse response,
        string requestId, long expectedRevision)
    {
        ClientStartupError? envelope = EnvelopeError(response, requestId, session.GameId);
        if (envelope is not null)
            return new(null, envelope, ClientMutationDisposition.Uncertain);
        if (response.Error is not null) return InterpretMutationError(response.Error);
        if (!HasCompleteGameplayResponse(response, allowWaiting: true)
            || expectedRevision == long.MaxValue
            || response.Revision != expectedRevision + 1)
            return new(null, Error("invalid_response",
                "The game service did not return a complete current table."),
                ClientMutationDisposition.Uncertain);
        client.RememberRevision(session, response);
        return new(new ClientResolutionResult(Sanitize(response), null,
            ClientMutationDisposition.Accepted, ClientSessionDisposition.Active),
            null, ClientMutationDisposition.Accepted);
    }

    private static MutationResponse InterpretMutationError(EngineError error)
    {
        if (!Complete(error)) return new(null,
            Error("invalid_response", "The game service returned an incomplete error."),
            ClientMutationDisposition.Uncertain);
        if (error.Code == "session_not_found") return new(
            UnavailableResolution(ClientMutationDisposition.Rejected), null,
            ClientMutationDisposition.Rejected);
        if (error.Code == "game_aborted") return new(
            UnavailableResolution(ClientMutationDisposition.Uncertain), null,
            ClientMutationDisposition.Uncertain);
        return new(null, Error(error.Code, error.Message), ClientMutationDisposition.Rejected);
    }

    private sealed record MutationResponse(
        ClientResolutionResult? Result,
        ClientStartupError? Failure,
        ClientMutationDisposition Mutation);

    internal static async ValueTask<ClientSynchronizationResult> SynchronizeAsync(
        this LocalGameClient client, ClientSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (SessionError(session) is not null)
        {
            return UnavailableSynchronization();
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            string requestId = client.NextRequestId(LocalGameClient.SynchronizeRequestId);
            EngineResponse response = await client.transport.ExchangeAsync(
                EngineRequest.SyncGame(
                    requestId, session.GameId, session.Capability),
                cancellationToken).ConfigureAwait(false);
            return SynchronizationResponse(client, session, response, requestId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return SynchronizationFailed(
                "transport_unavailable",
                "The current table could not be read. Try reconnecting again.");
        }
    }

    private static async ValueTask<ClientResolutionResult> RecoverCurrentViewAsync(
        this LocalGameClient client, ClientSession session,
        ClientStartupError failure,
        ClientMutationDisposition mutation)
    {
        string disposition = "rejected";
        string? errorCode = null;
        string requestId = client.NextRequestId(LocalGameClient.RecoverRequestId);
        try
        {
            EngineResponse synchronized = await client.transport.ExchangeAsync(
                EngineRequest.SyncGame(
                    requestId, session.GameId, session.Capability),
                CancellationToken.None).ConfigureAwait(false);
            return RecoveryResponse(client, session, failure, mutation, synchronized,
                requestId, ref disposition, ref errorCode);
        }
        catch (Exception)
        {
            disposition = "uncertain";
            errorCode = "transport_failed";
            return new ClientResolutionResult(
                Response: null, failure, mutation, ClientSessionDisposition.Active);
        }
        finally
        {
            if (mutation == ClientMutationDisposition.Uncertain)
            {
                client.log.Write(
                    OperationalEventIds.ReconnectCompleted,
                    disposition,
                    requestId: requestId,
                    gameId: session.GameId,
                    operation: "reconnect",
                    errorCode: errorCode);
            }
        }
    }

    private static ClientResolutionResult RecoveryResponse(
        LocalGameClient client, ClientSession session, ClientStartupError failure,
        ClientMutationDisposition mutation, EngineResponse response, string requestId,
        ref string disposition, ref string? errorCode)
    {
        ClientStartupError? envelope = EnvelopeError(response, requestId, session.GameId);
        if (envelope is not null)
        {
            errorCode = envelope.Code;
            return ActiveFailure(failure, mutation);
        }
        if (Unavailable(response.Error, out errorCode)) return UnavailableResolution(mutation);
        if (!CurrentResponse(client, session, response)) return ActiveFailure(failure, mutation);
        disposition = "accepted";
        client.RememberRevision(session, response);
        return new ClientResolutionResult(
            Sanitize(response), failure, mutation, ClientSessionDisposition.Active);
    }

    private static bool Unavailable(EngineError? error, out string? code)
    {
        code = null;
        if (error is null || !Complete(error)
            || error.Code is not ("session_not_found" or "game_aborted")) return false;
        code = error.Code;
        return true;
    }

    private static bool CurrentResponse(
        LocalGameClient client, ClientSession session, EngineResponse response) =>
        response.Error is null && HasCompleteSynchronizationResponse(response)
        && response.Revision >= client.RevisionFor(session);

    private static ClientResolutionResult ActiveFailure(
        ClientStartupError failure, ClientMutationDisposition mutation) =>
        new(null, failure, mutation, ClientSessionDisposition.Active);

    private static ClientSynchronizationResult SynchronizationResponse(
        LocalGameClient client, ClientSession session,
        EngineResponse response, string requestId)
    {
        ClientStartupError? envelope = EnvelopeError(response, requestId, session.GameId);
        if (envelope is not null)
            return new ClientSynchronizationResult(null, envelope, ClientSessionDisposition.Active);
        if (response.Error is not null) return SynchronizationError(response.Error);
        if (!HasCompleteSynchronizationResponse(response)
            || response.Revision < client.RevisionFor(session))
            return SynchronizationFailed("invalid_response",
                "The game service did not return a complete current table.");
        client.RememberRevision(session, response);
        return new ClientSynchronizationResult(
            Sanitize(response), Error: null, ClientSessionDisposition.Active);
    }

    private static ClientSynchronizationResult SynchronizationError(EngineError error)
    {
        if (!Complete(error)) return SynchronizationFailed(
            "invalid_response", "The game service returned an incomplete error.");
        return error.Code is "session_not_found" or "game_aborted"
            ? UnavailableSynchronization()
            : new ClientSynchronizationResult(null, Error(error.Code, error.Message),
                ClientSessionDisposition.Active);
    }
}
