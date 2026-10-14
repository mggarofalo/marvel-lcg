using Marvel.Server;
using static Marvel.Client.ClientResponseValidation;

namespace Marvel.Client;

/// <summary>Owns entry exchange and validation before installing session authority.</summary>
internal sealed class LocalGameEntryClient(LocalGameClient client)
{
    private const string AttachRequestId = "local-attach";
    private const string OpenRequestId = "local-open";

    /// <summary>Sends one canonical open request for an explicit opaque game id.</summary>
    internal async ValueTask<ClientEntryResult> OpenSessionAsync(
        string gameId,
        GameSpecification specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ClientStartupError? gameIdFailure = IdentifierError(
            gameId, "invalid_game_id", "Enter a game id from 1 through 256 characters.");
        if (gameIdFailure is not null)
        {
            return EntryFailed(gameIdFailure);
        }

        if (!ValidSpecification(specification))
        {
            return EntryFailed(Error(
                "invalid_selection",
                "Choose one or two distinct heroes in seat order."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await SendOpenSessionAsync(gameId, specification, cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask<ClientEntryResult> SendOpenSessionAsync(
        string gameId, GameSpecification specification, CancellationToken cancellationToken)
    {
        try
        {
            string requestId = client.NextRequestId(OpenRequestId);
            EngineResponse response = await client.transport.ExchangeAsync(
                EngineRequest.OpenGame(requestId, gameId, specification),
                cancellationToken).ConfigureAwait(false);
            return EntryResponse(response, requestId, gameId, allowWaiting: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return EntryFailed(Error(
                "transport_unavailable",
                "The game service could not be reached. Try starting the game again."));
        }
    }

    private static bool ValidSpecification(GameSpecification specification) =>
        specification.Heroes is { Count: 1 or 2 }
        && !specification.Heroes.Any(string.IsNullOrWhiteSpace)
        && specification.Heroes.Distinct(StringComparer.Ordinal).Count()
            == specification.Heroes.Count;

    /// <summary>Redeems a one-time seat invitation with exactly one request.</summary>
    internal async ValueTask<ClientEntryResult> AttachAsync(
        string gameId,
        string invitation,
        CancellationToken cancellationToken = default)
    {
        ClientStartupError? gameIdFailure = IdentifierError(
            gameId, "invalid_game_id", "Enter a game id from 1 through 256 characters.");
        if (gameIdFailure is not null)
        {
            return EntryFailed(gameIdFailure);
        }

        ClientStartupError? invitationFailure = IdentifierError(
            invitation,
            "invalid_invitation",
            "Enter a seat invitation from 1 through 256 characters.");
        if (invitationFailure is not null)
        {
            return EntryFailed(invitationFailure);
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            string requestId = client.NextRequestId(AttachRequestId);
            EngineResponse response = await client.transport.ExchangeAsync(
                EngineRequest.AttachGame(requestId, gameId, invitation),
                cancellationToken).ConfigureAwait(false);
            return EntryResponse(response, requestId, gameId, allowWaiting: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return EntryFailed(Error(
                "transport_unavailable",
                "The game service could not be reached. The invitation was not retried."));
        }
    }

    internal static ClientEntryResult EntryFailed(ClientStartupError error) =>
        new(Session: null, Response: null, Invitations: [], error);

    private ClientEntryResult EntryResponse(
        EngineResponse response,
        string requestId,
        string gameId,
        bool allowWaiting)
    {
        ClientStartupError? envelope = EnvelopeError(response, requestId, gameId);
        if (envelope is not null)
        {
            return EntryFailed(envelope);
        }

        if (response.Error is not null) return EntryError(response.Error, requestId);

        if (!CompleteEntry(response, requestId, allowWaiting))
        {
            return EntryFailed(Error(
                "invalid_response",
                "The engine did not return a complete initial game view."));
        }

        IReadOnlyList<SeatInvitation> invitations = response.Invitations ?? [];
        var session = new ClientSession(gameId, response.Capability!);
        client.RememberRevision(session, response);
        return new ClientEntryResult(
            session,
            Sanitize(response),
            invitations,
            Error: null);
    }

    private static ClientEntryResult EntryError(EngineError error, string requestId)
    {
        if (!Complete(error))
            return EntryFailed(Error("invalid_response",
                "The game service returned an incomplete error."));
        if (!HasRequestPrefix(requestId, AttachRequestId))
            return EntryFailed(Error(error.Code, error.Message));
        return EntryFailed(error.Code switch
        {
            "session_not_found" => Error("invitation_unavailable",
                "That seat invitation is unavailable. Ask the host for a new invitation."),
            "save_failed" => Error("save_failed",
                "The game service could not durably accept that seat invitation."),
            _ => Error("attach_failed",
                "The game service could not accept that seat invitation."),
        });
    }

    private static bool CompleteEntry(
        EngineResponse response, string requestId, bool allowWaiting) =>
        HasCompleteGameplayResponse(response, allowWaiting)
        && (allowWaiting || response.Prompt is not null)
        && (!HasRequestPrefix(requestId, OpenRequestId) || response.Revision == 0)
        && ValidIdentifier(response.Capability)
        && CompleteInvitations(response.Invitations)
        && (!allowWaiting || response.Invitations is not { Count: > 0 });
}
