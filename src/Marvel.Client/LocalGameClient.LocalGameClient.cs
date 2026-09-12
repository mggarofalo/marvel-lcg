using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

using static Marvel.Client.ClientResponseValidation;

namespace Marvel.Client;

/// <summary>The client-owned label for the one local table.</summary>

/// <summary>Uses the engine protocol for both local and remote game setup.</summary>
public sealed class LocalGameClient
{
    internal const int MaximumDisplayedErrorLength = 240;
    private const string AttachRequestId = "local-attach";
    private const string OpenRequestId = "local-open";
    internal const string RecoverRequestId = "local-recover";
    internal const string ResolveRequestId = "local-resolve";
    private const string SetupRequestId = "local-setup";
    internal const string SynchronizeRequestId = "local-sync";
    internal const string UndoRequestId = "local-undo";
    internal readonly Dictionary<ClientSession, HistoryDescriptor> histories = [];
    private readonly Dictionary<ClientSession, long> revisions = [];
    internal readonly IEngineTransport transport;
    internal readonly OperationalLog log;
    private readonly string? requestNonce;
    private readonly Func<uint> seedSource;
    private long requestSequence;

    /// <summary>Creates an app client over an embedded or remote transport.</summary>
    public LocalGameClient(
        IEngineTransport transport,
        OperationalLog? log = null,
        Func<uint>? seedSource = null)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.log = log ?? OperationalLog.None;
        this.seedSource = seedSource ?? GameSeed.Create;
        // Request ids are operational transport metadata and never enter the
        // deterministic game, save, replay, or RNG state.
        requestNonce = ReferenceEquals(this.log, OperationalLog.None)
            ? null
            : Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Composes the local host while keeping dataset access at its boundary.</summary>
    public static LocalClientConnection ConnectLocal(string dataRoot)
    {
        OperationalLog log = ClientComposition.ProcessLog;
        try
        {
            var host = new EngineHost(
                DatasetGameFactory.Load(dataRoot), log: log);
            return new LocalClientConnection(
                new LocalGameClient(new InProcessTransport(host), log), Error: null);
        }
        catch (Exception)
        {
            log.Write(
                OperationalEventIds.ServerStartFailed,
                "rejected",
                operation: "embedded_start",
                errorCode: "content_unavailable");
            return new LocalClientConnection(
                Client: null,
                Error(
                    "content_unavailable",
                    "The local game could not load its committed Core Set content."));
        }
    }

    /// <summary>Submits one answer without retrying an uncertain mutation.</summary>
    public ValueTask<ClientResolutionResult> ResolveAsync(
        string capability,
        EngineDecision decision,
        CancellationToken cancellationToken = default) =>
        LocalGameMutationClient.ResolveAsync(this, capability, decision, cancellationToken);

    /// <summary>Submits one answer for an explicit client session.</summary>
    public ValueTask<ClientResolutionResult> ResolveAsync(
        ClientSession session,
        EngineDecision decision,
        CancellationToken cancellationToken = default) =>
        LocalGameMutationClient.ResolveAsync(this, session, decision, cancellationToken);

    /// <summary>Moves an explicit session to an advertised history boundary.</summary>
    public ValueTask<ClientResolutionResult> UndoAsync(
        ClientSession session,
        int cursor,
        CancellationToken cancellationToken = default) =>
        LocalGameMutationClient.UndoAsync(this, session, cursor, cancellationToken);

    /// <summary>Reads one complete current view without mutating game state.</summary>
    public ValueTask<ClientSynchronizationResult> SynchronizeAsync(
        ClientSession session,
        CancellationToken cancellationToken = default) =>
        LocalGameMutationClient.SynchronizeAsync(this, session, cancellationToken);

    /// <summary>Reads the exact product choices accepted by the host.</summary>
    public async ValueTask<ClientSetupResult> ReadSetupAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            string requestId = NextRequestId(SetupRequestId);
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.ReadSetup(requestId), cancellationToken)
                .ConfigureAwait(false);
            return SetupResponse(response, requestId);
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

    private static ClientSetupResult SetupResponse(EngineResponse response, string requestId)
    {
        ClientStartupError? envelope = EnvelopeError(response, requestId, string.Empty);
        if (envelope is not null) return new ClientSetupResult(Choices: null, envelope);
        if (response.Error is not null) return SetupError(response.Error);
        if (!CompleteSetup(response.Setup) || !HasCompleteEvents(response.Events))
            return SetupFailed("invalid_response",
                "The game service did not return complete setup choices.");
        return new ClientSetupResult(response.Setup, Error: null);
    }

    private static ClientSetupResult SetupError(EngineError error) => Complete(error)
        ? SetupFailed(error.Code, error.Message)
        : SetupFailed("invalid_response", "The game service returned an incomplete error.");

    private static bool CompleteSetup(SetupChoices? choices)
    {
        if (choices?.Heroes is not { Count: > 0 }
            || choices.Scenarios is not { Count: > 0 }
            || choices.ModularSets is not { Count: > 0 }) return false;
        return !choices.Scenarios.SelectMany(scenario => scenario.RecommendedModularSets)
            .Any(recommended => !choices.ModularSets.Any(modular => modular.Key == recommended));
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

        if (!TryResolveModularSets(available, selection, out IReadOnlyList<string>? modularSets))
        {
            return ValueTask.FromResult(Failed(
                "invalid_selection",
                "Choose modular sets offered by this game service."));
        }

        if (!uint.TryParse(
                selection.Seed?.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint seed))
        {
            if (selection.Seed is not null && string.IsNullOrWhiteSpace(selection.Seed))
            {
                seed = seedSource();
            }
            else
            {
                return ValueTask.FromResult(Failed(
                    "invalid_seed",
                    "Enter a whole-number seed from 0 through 4294967295, or leave it blank."));
            }
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

        if (!TryResolveModularSets(available, selection, out IReadOnlyList<string>? modularSets))
        {
            return ValueTask.FromResult(EntryFailed(Error(
                "invalid_selection",
                "Choose modular sets offered by this game service.")));
        }

        if (!TrySeed(selection.Seed, out uint seed))
            return ValueTask.FromResult(EntryFailed(Error(
                "invalid_seed",
                "Enter a whole-number seed from 0 through 4294967295, or leave it blank.")));

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

    private bool TrySeed(string? text, out uint seed)
    {
        if (uint.TryParse(text?.Trim(), NumberStyles.None,
                CultureInfo.InvariantCulture, out seed)) return true;
        if (text is null || !string.IsNullOrWhiteSpace(text)) return false;
        seed = seedSource();
        return true;
    }

    private async ValueTask<ClientEntryResult> SendOpenSessionAsync(
        string gameId, GameSpecification specification, CancellationToken cancellationToken)
    {
        try
        {
            string requestId = NextRequestId(OpenRequestId);
            EngineResponse response = await transport.ExchangeAsync(
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
            string requestId = NextRequestId(AttachRequestId);
            EngineResponse response = await transport.ExchangeAsync(
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

        if (response.Error is not null) return EntryError(response.Error, requestId);

        if (!CompleteEntry(response, requestId, allowWaiting))
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


    internal string NextRequestId(string prefix) => requestNonce is null
        ? prefix
        : $"{prefix}-{requestNonce}-{Interlocked.Increment(ref requestSequence)}";


    internal long RevisionFor(ClientSession session) =>
        revisions.TryGetValue(session, out long revision) ? revision : 0;

    internal void RememberRevision(ClientSession session, EngineResponse response)
    {
        revisions[session] = response.Revision;
        if (response.History is not null)
        {
            histories[session] = response.History;
        }
    }

}
