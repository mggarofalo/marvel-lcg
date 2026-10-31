using Marvel.Protocol;
using static Marvel.Client.ClientResponseValidation;

namespace Marvel.Client;

/// <summary>Validates read-only replacement views and authoritative session unavailability.</summary>
internal static class LocalGameSynchronizationClient
{
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
        return error.Disposition == EngineFailureDisposition.SessionUnavailable
            ? UnavailableSynchronization(error)
            : new ClientSynchronizationResult(null, Error(error.Code, error.Message),
                ClientSessionDisposition.Active);
    }
}
