using Marvel.Observability;
using Marvel.Protocol;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

using static Marvel.Client.ClientResponseValidation;

namespace Marvel.Client;

/// <summary>Uses the engine protocol for both local and remote game setup.</summary>
public sealed class LocalGameClient : IDisposable, IAsyncDisposable
{
    internal const int MaximumDisplayedErrorLength = 240;
    internal const string RecoverRequestId = "local-recover";
    internal const string ResolveRequestId = "local-resolve";
    private const string SetupRequestId = "local-setup";
    internal const string SynchronizeRequestId = "local-sync";
    internal const string UndoRequestId = "local-undo";
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ClientSession, ClientSessionSnapshot> snapshots = new();
    internal readonly IEngineTransport transport;
    internal readonly OperationalLog log;
    private readonly string? requestNonce;
    private readonly Func<uint> seedSource;
    private long requestSequence;
    private readonly LocalGameEntryClient entry;

    /// <summary>Creates an app client over an embedded or remote transport.</summary>
    public LocalGameClient(
        IEngineTransport transport,
        OperationalLog? log = null,
        Func<uint>? seedSource = null)
    {
        entry = new LocalGameEntryClient(this);
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.log = log ?? OperationalLog.None;
        this.seedSource = seedSource ?? GameSeed.Create;
        // Request ids are operational transport metadata and never enter the
        // deterministic game, save, replay, or RNG state.
        requestNonce = ReferenceEquals(this.log, OperationalLog.None)
            ? null
            : Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Retries the exact entry claim retained after an uncertain open or attach response.</summary>
    public ValueTask<ClientEntryResult> RetryEntryAsync(ClientEntryAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        return entry.SendAttemptAsync(attempt, cancellationToken);
    }

    /// <summary>Requests transport shutdown without blocking the presentation thread.</summary>
    public void Dispose()
    {
        if (transport is IDisposable lifetime) lifetime.Dispose();
    }

    /// <summary>Awaits any accepted embedded exchanges before transport shutdown completes.</summary>
    public ValueTask DisposeAsync()
    {
        if (transport is IAsyncDisposable lifetime) return lifetime.DisposeAsync();
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>Submits one answer for an explicit client session.</summary>
    public ValueTask<ClientResolutionResult> ResolveAsync(
        ClientSession session,
        Decision decision,
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
        LocalGameSynchronizationClient.SynchronizeAsync(this, session, cancellationToken);

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

    /// <summary>
    /// Validates a setup selection and opens the explicitly named game with exactly one mutation request.
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
            return ValueTask.FromResult(LocalGameEntryClient.EntryFailed(gameIdFailure));
        }

        if (!ValidHeroSelection(available, selection.HeroKeys)
            || !available.Scenarios.Any(scenario => scenario.Key == selection.ScenarioKey))
        {
            return ValueTask.FromResult(LocalGameEntryClient.EntryFailed(Error(
                "invalid_selection",
                "Choose one through four distinct heroes and a scenario offered by this game service.")));
        }

        if (!TryResolveModularSets(available, selection, out IReadOnlyList<string>? modularSets))
        {
            return ValueTask.FromResult(LocalGameEntryClient.EntryFailed(Error(
                "invalid_selection",
                "Choose modular sets offered by this game service.")));
        }

        if (!TrySeed(selection.Seed, out uint seed))
            return ValueTask.FromResult(LocalGameEntryClient.EntryFailed(Error(
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
    public ValueTask<ClientEntryResult> OpenSessionAsync(
        string gameId, GameSpecification specification, CancellationToken cancellationToken = default) =>
        entry.OpenSessionAsync(gameId, specification, cancellationToken);

    private bool TrySeed(string? text, out uint seed)
    {
        if (uint.TryParse(text?.Trim(), NumberStyles.None,
                CultureInfo.InvariantCulture, out seed)) return true;
        if (text is null || !string.IsNullOrWhiteSpace(text)) return false;
        seed = seedSource();
        return true;
    }

    /// <summary>Redeems a one-time seat invitation with exactly one request.</summary>
    public ValueTask<ClientEntryResult> AttachAsync(
        string gameId, string invitation, CancellationToken cancellationToken = default) =>
        entry.AttachAsync(gameId, invitation, cancellationToken);

    private static ClientSetupResult SetupFailed(string code, string message) =>
        new(Choices: null, Error(code, message));

    internal string NextRequestId(string prefix) => requestNonce is null
        ? prefix
        : $"{prefix}-{requestNonce}-{Interlocked.Increment(ref requestSequence)}";


    internal long RevisionFor(ClientSession session) =>
        snapshots.TryGetValue(session, out var snapshot) ? snapshot.Revision : 0;

    internal HistoryDescriptor? HistoryFor(ClientSession session) =>
        snapshots.TryGetValue(session, out var snapshot) ? snapshot.History : null;

    internal void RememberRevision(ClientSession session, EngineResponse response)
    {
        var incoming = new ClientSessionSnapshot(response.Revision, response.History);
        snapshots.AddOrUpdate(session, incoming,
            (_, current) => incoming.Revision >= current.Revision ? incoming : current);
    }
}
