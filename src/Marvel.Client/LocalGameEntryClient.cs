using Marvel.Protocol;
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
                "Choose one through four distinct heroes in seat order."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await SendOpenSessionAsync(gameId, specification, cancellationToken)
            .ConfigureAwait(false);
    }

    private ValueTask<ClientEntryResult> SendOpenSessionAsync(
        string gameId, GameSpecification specification, CancellationToken cancellationToken) =>
        SendAttemptAsync(new ClientEntryAttempt(EngineRequest.OpenGame(
            client.NextRequestId(OpenRequestId), gameId, specification)), cancellationToken);

    internal async ValueTask<ClientEntryResult> SendAttemptAsync(
        ClientEntryAttempt attempt, CancellationToken cancellationToken)
    {
        EngineRequest request = attempt.Request;
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            EngineResponse response = await client.transport.ExchangeAsync(request, cancellationToken).ConfigureAwait(false);
            ClientEntryResult result = EntryResponse(response, request.RequestId, request.GameId,
                allowWaiting: request.Operation == EngineProtocol.Attach);
            return result.Succeeded || DefinitiveFailure(response, request) ? result : result with { PendingEntry = attempt };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return EntryFailed(Error("entry_uncertain", "Entry was interrupted. Recover this attempt before starting another."))
                with { PendingEntry = attempt };
        }
        catch (Exception)
        {
            return EntryFailed(Error("transport_unavailable", "The entry response was lost. Retry this same attempt to recover it."))
                with { PendingEntry = attempt };
        }
    }

    private static bool DefinitiveFailure(EngineResponse response, EngineRequest request) =>
        EnvelopeError(response, request.RequestId, request.GameId) is null
        && response.Error is { } error && Complete(error)
        && error.Disposition != EngineFailureDisposition.CommitUncertain;

    private static bool ValidSpecification(GameSpecification specification) =>
        specification.Heroes is not null
        && specification.Heroes.Count is >= Marvel.Rules.Setup.PlayerCountRules.Minimum and <= Marvel.Rules.Setup.PlayerCountRules.Maximum
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
        return await SendAttemptAsync(new ClientEntryAttempt(EngineRequest.AttachGame(
            client.NextRequestId(AttachRequestId), gameId, invitation)), cancellationToken).ConfigureAwait(false);
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
            "save_incompatible" => Error(error.Code, error.Message),
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
        && ValidIdentifier(response.Capability)
        && CompleteInvitations(response.Invitations)
        && (!allowWaiting || response.Invitations is not { Count: > 0 });
}
