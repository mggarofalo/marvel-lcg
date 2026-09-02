using System.Globalization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The client-owned label for the one local table.</summary>
public static class LocalGameSession
{
    /// <summary>The opaque game id sent through either transport.</summary>
    public const string GameId = "local-core-game";
}

/// <summary>A bounded product-level failure suitable for display.</summary>
public sealed record ClientStartupError(string Code, string Message);

/// <summary>A composed client, or why its engine connection could not be configured.</summary>
public sealed record LocalClientConnection(
    LocalGameClient? Client,
    ClientStartupError? Error)
{
    /// <summary>Whether the configured engine transport is available.</summary>
    public bool Succeeded => Client is not null && Error is null;
}

/// <summary>The result of discovering the authored setup surface.</summary>
public sealed record ClientSetupResult(
    SetupChoices? Choices,
    ClientStartupError? Error)
{
    /// <summary>Whether complete choices were returned.</summary>
    public bool Succeeded => Choices is not null && Error is null;
}

/// <summary>The opaque identifiers required to continue one authorized game view.</summary>
public sealed record ClientSession(string GameId, string Capability);

/// <summary>
/// A newly opened or attached session, its render-safe view, and any invitations
/// that the entry screen must hand off before discarding them.
/// </summary>
public sealed record ClientEntryResult(
    ClientSession? Session,
    EngineResponse? Response,
    IReadOnlyList<SeatInvitation> Invitations,
    ClientStartupError? Error)
{
    /// <summary>Whether a complete authorized game view was returned.</summary>
    public bool Succeeded => Session is not null && Response is not null && Error is null;
}

/// <summary>The result of trying to open one selected game.</summary>
public sealed record ClientStartupResult(
    EngineResponse? Response,
    ClientStartupError? Error)
{
    /// <summary>Whether a complete initial game view was returned.</summary>
    public bool Succeeded => Response is not null && Error is null;
}

/// <summary>
/// What the client can prove happened to one submitted mutation. These states
/// are a client contract chosen by this project; the game rules do not define transport recovery.
/// </summary>
public enum ClientMutationDisposition
{
    /// <summary>The request did not reach the game service.</summary>
    NotSent,

    /// <summary>The game service accepted the decision.</summary>
    Accepted,

    /// <summary>The game service refused the decision without applying it.</summary>
    Rejected,

    /// <summary>The client cannot prove whether the decision was applied.</summary>
    Uncertain,
}

/// <summary>Whether the bearer session can still be used.</summary>
public enum ClientSessionDisposition
{
    /// <summary>The session remains available for requests.</summary>
    Active,

    /// <summary>The service has established that the session is unavailable.</summary>
    Unavailable,
}

/// <summary>A resolved decision, optionally paired with a recovered current view.</summary>
public sealed record ClientResolutionResult(
    EngineResponse? Response,
    ClientStartupError? Error,
    ClientMutationDisposition MutationDisposition = ClientMutationDisposition.Accepted,
    ClientSessionDisposition SessionDisposition = ClientSessionDisposition.Active)
{
    /// <summary>Whether the submitted decision was accepted.</summary>
    public bool Succeeded => MutationDisposition == ClientMutationDisposition.Accepted
        && Response is not null
        && Error is null;

    /// <summary>Whether an authoritative view is available for rendering.</summary>
    public bool HasAuthoritativeView => Response is not null;
}

/// <summary>The result of reading the current authoritative session view.</summary>
public sealed record ClientSynchronizationResult(
    EngineResponse? Response,
    ClientStartupError? Error,
    ClientSessionDisposition SessionDisposition)
{
    /// <summary>Whether a complete current view was returned.</summary>
    public bool Succeeded => Response is not null && Error is null;

    /// <summary>Whether an authoritative view is available for rendering.</summary>
    public bool HasAuthoritativeView => Response is not null;
}

/// <summary>How the player wants to fill the scenario's modular-set slot.</summary>
public enum ModularConfiguration
{
    /// <summary>Use the scenario's authored recommendation.</summary>
    Recommended,

    /// <summary>Deliberately include no modular set.</summary>
    None,

    /// <summary>Use one explicitly selected authored modular set.</summary>
    Selected,
}

/// <summary>Raw values selected by the setup screen.</summary>
public sealed record GameSetupSelection(
    IReadOnlyList<string> HeroKeys,
    string ScenarioKey,
    ModularConfiguration Modular,
    string? ModularKey,
    string Seed)
{
    /// <summary>Creates the single-hero selection used by the local-play screen.</summary>
    public GameSetupSelection(
        string heroKey,
        string scenarioKey,
        ModularConfiguration modular,
        string? modularKey,
        string seed)
        : this([heroKey], scenarioKey, modular, modularKey, seed)
    {
    }
}

/// <summary>Uses the engine protocol for both local and remote game setup.</summary>
public sealed class LocalGameClient
{
    private const int MaximumDisplayedErrorLength = 240;
    private const string AttachRequestId = "local-attach";
    private const string OpenRequestId = "local-open";
    private const string RecoverRequestId = "local-recover";
    private const string ResolveRequestId = "local-resolve";
    private const string SetupRequestId = "local-setup";
    private const string SynchronizeRequestId = "local-sync";
    private readonly Dictionary<ClientSession, long> revisions = [];
    private readonly IEngineTransport transport;

    /// <summary>Creates an app client over an embedded or remote transport.</summary>
    public LocalGameClient(IEngineTransport transport) =>
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));

    /// <summary>Composes the local host while keeping dataset access at its boundary.</summary>
    public static LocalClientConnection ConnectLocal(string dataRoot)
    {
        try
        {
            var host = new EngineHost(DatasetGameFactory.Load(dataRoot));
            return new LocalClientConnection(
                new LocalGameClient(new InProcessTransport(host)), Error: null);
        }
        catch (Exception)
        {
            return new LocalClientConnection(
                Client: null,
                Error(
                    "content_unavailable",
                    "The local game could not load its committed Core Set content."));
        }
    }

    /// <summary>Reads the exact product choices accepted by the host.</summary>
    public async ValueTask<ClientSetupResult> ReadSetupAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.ReadSetup(SetupRequestId), cancellationToken)
                .ConfigureAwait(false);
            ClientStartupError? envelope = EnvelopeError(
                response, SetupRequestId, string.Empty);
            if (envelope is not null)
            {
                return new ClientSetupResult(Choices: null, envelope);
            }

            if (response.Error is not null)
            {
                if (!Complete(response.Error))
                {
                    return SetupFailed(
                        "invalid_response",
                        "The game service returned an incomplete error.");
                }

                return SetupFailed(response.Error.Code, response.Error.Message);
            }

            SetupChoices? choices = response.Setup;
            if (choices?.Heroes is not { Count: > 0 }
                || choices.Scenarios is not { Count: > 0 }
                || choices.ModularSets is not { Count: > 0 }
                || choices.Scenarios
                    .SelectMany(scenario => scenario.RecommendedModularSets)
                    .Any(recommended => !choices.ModularSets.Any(
                        modular => modular.Key == recommended))
                || !HasCompleteEvents(response.Events))
            {
                return SetupFailed(
                    "invalid_response",
                    "The game service did not return complete setup choices.");
            }

            return new ClientSetupResult(choices, Error: null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return SetupFailed(
                "transport_unavailable",
                "The game service could not be reached. Try loading setup again.");
        }
    }

    /// <summary>Validates a screen selection and sends exactly one open request.</summary>
    public ValueTask<ClientStartupResult> OpenAsync(
        SetupChoices available,
        GameSetupSelection selection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(available);
        ArgumentNullException.ThrowIfNull(selection);

        if (!ValidHeroSelection(available, selection.HeroKeys)
            || !available.Scenarios.Any(scenario => scenario.Key == selection.ScenarioKey))
        {
            return ValueTask.FromResult(Failed(
                "invalid_selection",
                "Choose a hero and scenario offered by this game service."));
        }

        IReadOnlyList<string>? modularSets;
        switch (selection.Modular)
        {
            case ModularConfiguration.Recommended:
                modularSets = null;
                break;
            case ModularConfiguration.None:
                modularSets = [];
                break;
            case ModularConfiguration.Selected
                when selection.ModularKey is not null
                     && available.ModularSets.Any(set => set.Key == selection.ModularKey):
                modularSets = [selection.ModularKey];
                break;
            default:
                return ValueTask.FromResult(Failed(
                    "invalid_selection",
                    "Choose a modular-set option offered by this game service."));
        }

        if (!uint.TryParse(
                selection.Seed,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint seed))
        {
            return ValueTask.FromResult(Failed(
                "invalid_seed",
                "Enter a whole-number seed from 0 through 4294967295."));
        }

        return OpenAsync(
            new GameSpecification(
                selection.ScenarioKey,
                selection.HeroKeys,
                modularSets,
                seed),
            cancellationToken);
    }

    /// <summary>
    /// Validates a setup selection and opens the explicitly named one- or
    /// two-seat game with exactly one mutation request.
    /// </summary>
    public ValueTask<ClientEntryResult> OpenSessionAsync(
        string gameId,
        SetupChoices available,
        GameSetupSelection selection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(available);
        ArgumentNullException.ThrowIfNull(selection);

        ClientStartupError? gameIdFailure = IdentifierError(
            gameId, "invalid_game_id", "Enter a game id from 1 through 256 characters.");
        if (gameIdFailure is not null)
        {
            return ValueTask.FromResult(EntryFailed(gameIdFailure));
        }

        if (!ValidHeroSelection(available, selection.HeroKeys)
            || !available.Scenarios.Any(scenario => scenario.Key == selection.ScenarioKey))
        {
            return ValueTask.FromResult(EntryFailed(Error(
                "invalid_selection",
                "Choose one or two distinct heroes and a scenario offered by this game service.")));
        }

        IReadOnlyList<string>? modularSets;
        switch (selection.Modular)
        {
            case ModularConfiguration.Recommended:
                modularSets = null;
                break;
            case ModularConfiguration.None:
                modularSets = [];
                break;
            case ModularConfiguration.Selected
                when selection.ModularKey is not null
                     && available.ModularSets.Any(set => set.Key == selection.ModularKey):
                modularSets = [selection.ModularKey];
                break;
            default:
                return ValueTask.FromResult(EntryFailed(Error(
                    "invalid_selection",
                    "Choose a modular-set option offered by this game service.")));
        }

        if (!uint.TryParse(
                selection.Seed,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint seed))
        {
            return ValueTask.FromResult(EntryFailed(Error(
                "invalid_seed",
                "Enter a whole-number seed from 0 through 4294967295.")));
        }

        return OpenSessionAsync(
            gameId,
            new GameSpecification(
                selection.ScenarioKey,
                selection.HeroKeys,
                modularSets,
                seed),
            cancellationToken);
    }

    /// <summary>Sends one canonical open request for an explicit opaque game id.</summary>
    public async ValueTask<ClientEntryResult> OpenSessionAsync(
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

        if (specification.Heroes is not { Count: 1 or 2 }
            || specification.Heroes.Any(string.IsNullOrWhiteSpace)
            || specification.Heroes.Distinct(StringComparer.Ordinal).Count()
                != specification.Heroes.Count)
        {
            return EntryFailed(Error(
                "invalid_selection",
                "Choose one or two distinct heroes in seat order."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.OpenGame(OpenRequestId, gameId, specification),
                cancellationToken).ConfigureAwait(false);
            return EntryResponse(response, OpenRequestId, gameId, allowWaiting: false);
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

    /// <summary>Redeems a one-time seat invitation with exactly one request.</summary>
    public async ValueTask<ClientEntryResult> AttachAsync(
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
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.AttachGame(AttachRequestId, gameId, invitation),
                cancellationToken).ConfigureAwait(false);
            return EntryResponse(response, AttachRequestId, gameId, allowWaiting: true);
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

    /// <summary>Sends the canonical open request through the configured transport.</summary>
    public async ValueTask<ClientStartupResult> OpenAsync(
        GameSpecification specification,
        CancellationToken cancellationToken = default)
    {
        ClientEntryResult entry = await OpenSessionAsync(
            LocalGameSession.GameId, specification, cancellationToken).ConfigureAwait(false);
        EngineResponse? legacyResponse = entry.Response is null || entry.Session is null
            ? null
            : entry.Response with
            {
                Capability = entry.Session.Capability,
                Invitations = entry.Invitations,
            };
        return new ClientStartupResult(legacyResponse, entry.Error);
    }

    /// <summary>
    /// Submits one answer and, after a rejection or uncertain response, reads
    /// the current view without ever repeating the mutation.
    /// </summary>
    public async ValueTask<ClientResolutionResult> ResolveAsync(
        string capability,
        EngineDecision decision,
        CancellationToken cancellationToken = default)
    {
        return await ResolveAsync(
            new ClientSession(LocalGameSession.GameId, capability),
            decision,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Submits one answer for an explicit session and recovers that same
    /// session without ever repeating the mutation.
    /// </summary>
    public async ValueTask<ClientResolutionResult> ResolveAsync(
        ClientSession session,
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
        long expectedRevision = RevisionFor(session);
        try
        {
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.ResolveGame(
                    ResolveRequestId,
                    session.GameId,
                    session.Capability,
                    decision,
                    expectedRevision),
                cancellationToken).ConfigureAwait(false);
            ClientStartupError? envelope = EnvelopeError(
                response, ResolveRequestId, session.GameId);
            if (envelope is not null)
            {
                failure = envelope;
            }
            else if (response.Error is null)
            {
                if (HasCompleteGameplayResponse(response, allowWaiting: true)
                    && expectedRevision < long.MaxValue
                    && response.Revision == expectedRevision + 1)
                {
                    RememberRevision(session, response);
                    return new ClientResolutionResult(
                        Sanitize(response),
                        Error: null,
                        ClientMutationDisposition.Accepted,
                        ClientSessionDisposition.Active);
                }

                failure = Error(
                    "invalid_response",
                    "The game service did not return a complete current table.");
            }
            else
            {
                if (!Complete(response.Error))
                {
                    failure = Error(
                        "invalid_response",
                        "The game service returned an incomplete error.");
                }
                else if (response.Error.Code == "session_not_found")
                {
                    return UnavailableResolution(ClientMutationDisposition.Rejected);
                }
                else if (response.Error.Code == "game_aborted")
                {
                    return UnavailableResolution(ClientMutationDisposition.Uncertain);
                }
                else
                {
                    mutation = ClientMutationDisposition.Rejected;
                    failure = Error(response.Error.Code, response.Error.Message);
                }
            }
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

        return await RecoverCurrentViewAsync(session, failure, mutation)
            .ConfigureAwait(false);
    }

    /// <summary>Reads one complete current view without mutating game state.</summary>
    public async ValueTask<ClientSynchronizationResult> SynchronizeAsync(
        ClientSession session,
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
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.SyncGame(
                    SynchronizeRequestId, session.GameId, session.Capability),
                cancellationToken).ConfigureAwait(false);
            ClientStartupError? envelope = EnvelopeError(
                response, SynchronizeRequestId, session.GameId);
            if (envelope is not null)
            {
                return new ClientSynchronizationResult(
                    Response: null, envelope, ClientSessionDisposition.Active);
            }

            if (response.Error is not null)
            {
                if (!Complete(response.Error))
                {
                    return SynchronizationFailed(
                        "invalid_response",
                        "The game service returned an incomplete error.");
                }

                return response.Error.Code is "session_not_found" or "game_aborted"
                    ? UnavailableSynchronization()
                    : new ClientSynchronizationResult(
                        Response: null,
                        Error(response.Error.Code, response.Error.Message),
                        ClientSessionDisposition.Active);
            }

            if (!HasCompleteSynchronizationResponse(response)
                || response.Revision < RevisionFor(session))
            {
                return SynchronizationFailed(
                    "invalid_response",
                    "The game service did not return a complete current table.");
            }

            RememberRevision(session, response);
            return new ClientSynchronizationResult(
                Sanitize(response), Error: null, ClientSessionDisposition.Active);
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

    private static ClientSetupResult SetupFailed(string code, string message) =>
        new(Choices: null, Error(code, message));

    private static ClientStartupResult Failed(string code, string message) =>
        new(Response: null, Error(code, message));

    private static ClientEntryResult EntryFailed(ClientStartupError error) =>
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

        if (response.Error is not null)
        {
            if (!Complete(response.Error))
            {
                return EntryFailed(Error(
                    "invalid_response",
                    "The game service returned an incomplete error."));
            }

            if (requestId == AttachRequestId)
            {
                return EntryFailed(response.Error.Code == "session_not_found"
                    ? Error(
                    "invitation_unavailable",
                    "That seat invitation is unavailable. Ask the host for a new invitation.")
                    : Error(
                        "attach_failed",
                        "The game service could not accept that seat invitation."));
            }

            return EntryFailed(Error(response.Error.Code, response.Error.Message));
        }

        if (!HasCompleteGameplayResponse(response, allowWaiting)
            || !allowWaiting && response.Prompt is null
            || requestId == OpenRequestId && response.Revision != 0
            || !ValidIdentifier(response.Capability)
            || !CompleteInvitations(response.Invitations)
            || allowWaiting && response.Invitations is { Count: > 0 })
        {
            return EntryFailed(Error(
                "invalid_response",
                "The engine did not return a complete initial game view."));
        }

        IReadOnlyList<SeatInvitation> invitations = response.Invitations ?? [];
        var session = new ClientSession(gameId, response.Capability!);
        RememberRevision(session, response);
        return new ClientEntryResult(
            session,
            Sanitize(response),
            invitations,
            Error: null);
    }

    private async ValueTask<ClientResolutionResult> RecoverCurrentViewAsync(
        ClientSession session,
        ClientStartupError failure,
        ClientMutationDisposition mutation)
    {
        try
        {
            EngineResponse synchronized = await transport.ExchangeAsync(
                EngineRequest.SyncGame(
                    RecoverRequestId, session.GameId, session.Capability),
                CancellationToken.None).ConfigureAwait(false);
            ClientStartupError? envelope = EnvelopeError(
                synchronized, RecoverRequestId, session.GameId);
            if (envelope is not null)
            {
                return new ClientResolutionResult(
                    Response: null, failure, mutation, ClientSessionDisposition.Active);
            }

            if (synchronized.Error is { } syncError
                && Complete(syncError)
                && syncError.Code is "session_not_found" or "game_aborted")
            {
                return UnavailableResolution(mutation);
            }

            if (synchronized.Error is null
                && HasCompleteSynchronizationResponse(synchronized)
                && synchronized.Revision >= RevisionFor(session))
            {
                RememberRevision(session, synchronized);
                return new ClientResolutionResult(
                    Sanitize(synchronized), failure, mutation, ClientSessionDisposition.Active);
            }

            return new ClientResolutionResult(
                Response: null, failure, mutation, ClientSessionDisposition.Active);
        }
        catch (Exception)
        {
            return new ClientResolutionResult(
                Response: null, failure, mutation, ClientSessionDisposition.Active);
        }
    }

    private static ClientResolutionResult UnavailableResolution(
        ClientMutationDisposition mutation) =>
        new(
            Response: null,
            Error(
                "session_unavailable",
                "This game session is unavailable. Return to the connection screen."),
            mutation,
            ClientSessionDisposition.Unavailable);

    private static ClientSynchronizationResult UnavailableSynchronization() =>
        new(
            Response: null,
            Error(
                "session_unavailable",
                "This game session is unavailable. Return to the connection screen."),
            ClientSessionDisposition.Unavailable);

    private static ClientSynchronizationResult SynchronizationFailed(
        string code,
        string message) =>
        new(Response: null, Error(code, message), ClientSessionDisposition.Active);

    private static ClientStartupError Error(string code, string message) =>
        new(Bounded(code), Bounded(message));

    private static ClientStartupError? IdentifierError(
        string? value,
        string code,
        string message) =>
        !ValidIdentifier(value)
            ? Error(code, message)
            : null;

    private static bool ValidIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= EngineProtocol.MaximumIdentifierLength;

    private static ClientStartupError? SessionError(ClientSession session) =>
        IdentifierError(
            session.GameId,
            "invalid_session",
            "The saved game session is incomplete.")
        ?? IdentifierError(
            session.Capability,
            "invalid_session",
            "The saved game session is incomplete.");

    private static EngineResponse Sanitize(EngineResponse response) =>
        response with { Capability = null, Invitations = null };

    private long RevisionFor(ClientSession session) =>
        revisions.TryGetValue(session, out long revision) ? revision : 0;

    private void RememberRevision(ClientSession session, EngineResponse response) =>
        revisions[session] = response.Revision;

    private static bool ValidHeroSelection(
        SetupChoices available,
        IReadOnlyList<string>? heroKeys) =>
        heroKeys is { Count: 1 or 2 }
        && heroKeys.All(key =>
            !string.IsNullOrWhiteSpace(key)
            && available.Heroes.Any(hero => hero.Key == key))
        && heroKeys.Distinct(StringComparer.Ordinal).Count() == heroKeys.Count;

    private static bool CompleteInvitations(IReadOnlyList<SeatInvitation>? invitations) =>
        invitations is null
        || invitations.All(invitation =>
            invitation is not null
            && invitation.Seat >= 0
            && ValidIdentifier(invitation.Invitation))
        && invitations.Select(invitation => invitation.Seat).Distinct().Count()
            == invitations.Count
        && invitations.Select(invitation => invitation.Invitation)
            .Distinct(StringComparer.Ordinal).Count() == invitations.Count;

    private static ClientStartupError? EnvelopeError(
        EngineResponse response,
        string requestId,
        string gameId)
    {
        if (response.Version != EngineProtocol.Version)
        {
            return Error(
                "unsupported_version",
                "The game service uses an unsupported protocol version.");
        }

        return response.RequestId == requestId && response.GameId == gameId
            ? null
            : Error(
                "invalid_response",
                "The game service returned a response for a different request or game.");
    }

    private static bool HasCompleteBoard(WorldDescriptor? world) =>
        world?.Players is not null
        && world.Areas is not null
        && world.GameAreas is not null
        && world.Players.All(player => player is not null && player.Name is not null)
        && world.GameAreas.All(area => area is not null && area.PlayAreas is not null)
        && world.Areas.All(area =>
            area is not null
            && area.Zone is not null
            && area.Cards is not null
            && area.Removed is not null
            && area.Cards.Concat(area.Removed).All(card =>
                card is not null
                && (card.Face is null
                    || card.Face.Id is not null
                    && card.Face.Title is not null
                    && card.Face.Subtitle is not null
                    && card.Face.Fields is not null)));

    private static bool HasCompleteGameplayResponse(
        EngineResponse response,
        bool allowWaiting = false) =>
        response.Revision >= 0
        && HasCompleteEvents(response.Events)
        && HasCompleteBoard(response.World)
        && Enum.IsDefined(response.World!.Outcome)
        && (response.World.Outcome == Outcome.Unfinished
            ? (allowWaiting && response.Prompt is null) || HasCompletePrompt(response.Prompt)
            : response.Prompt is null);

    private static bool HasCompleteSynchronizationResponse(EngineResponse response) =>
        response.Events is { Count: 0 }
        && HasCompleteGameplayResponse(response, allowWaiting: true);

    private static bool HasCompleteEvents(IReadOnlyList<GameEvent>? events) =>
        events is not null && events.All(happened =>
            happened is not null
            && happened.Trigger is not null
            && happened.Verb is not null
            && happened switch
            {
                CardsCreated created => Complete(created.Area)
                    && created.Cards is not null
                    && created.Cards.All(card => card.Card is not null),
                CardsMoved moved => Complete(moved.From)
                    && Complete(moved.To)
                    && moved.Cards is not null,
                AreaReordered reordered => Complete(reordered.Area)
                    && reordered.Order is not null,
                CardFormChanged changed => changed.From is not null
                    && changed.To is not null,
                CardsFlipped flipped => flipped.Cards is not null,
                CardAttached => true,
                CardDetached => true,
                ControlChanged => true,
                PlayAreaJoined => true,
                PlayAreaDetached => true,
                FieldSet set => set.Field is not null,
                _ => false,
            });

    private static bool Complete(AreaRef area) =>
        area.Zone is not null && area.Id is not null;

    private static bool Complete(EngineError error) =>
        error.Code is not null && error.Message is not null;

    private static bool HasCompletePrompt(Prompt? prompt) =>
        prompt?.Trigger is not null
        && prompt.Label is not null
        && prompt.Affordances is { Count: > 0 }
        && prompt.Affordances.All(option =>
            option is not null
            && option.Verb is not null
            && option.Label is not null
            && (option.Targets is null
                || option.Targets.Legal is not null
                && (option.Targets.Groups is null
                    || option.Targets.Groups.All(group => group is not null))
                && (option.Targets.MustIncludeTraits is null
                    || option.Targets.MustIncludeTraits.All(trait => trait is not null)))
            && (option.Costs is null || option.Costs.All(cost =>
                cost is not null
                && cost.Cost is not null
                && cost.OrCost is not null
                && (cost.Rule is null || cost.Rule.All(rule => rule is not null))
                && (cost.OrRule is null || cost.OrRule.All(rule => rule is not null))
                && (cost.Sources is null
                    || cost.Sources.All(source => source.Generates is not null))
                && (cost.Variables is null
                    || cost.Variables.All(variable => variable.Name is not null))
                && (cost.Components is null || cost.Components.All(component =>
                    component is not null
                    && component.Cost is not null
                    && (component.Rule is null
                        || component.Rule.All(rule => rule is not null)))))));

    private static string Bounded(string value) =>
        value.Length <= MaximumDisplayedErrorLength
            ? value
            : value[..MaximumDisplayedErrorLength];
}
