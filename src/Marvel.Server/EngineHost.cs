using Marvel.Observability;
using Marvel.Protocol;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.View;
using System.Diagnostics;
using System.Security.Cryptography;

using static Marvel.Server.EngineHostLifecycle;

namespace Marvel.Server;

/// <summary>Hosts independent deterministic sessions with serialized access to each game.</summary>
/// <remarks>Network I/O takes place outside session gates. Every request reauthorizes
/// after acquiring its session gate, so concurrent revocation cannot admit a stale capability.</remarks>
public sealed class EngineHost : IEngineEndpoint
{
    private readonly object[] entryGates = Enumerable.Range(0, 64).Select(_ => new object()).ToArray();
    internal readonly IGameFactory factory;
    internal readonly IVisibilityPolicy visibility;
    internal readonly ISessionStore store;
    internal readonly SessionCompatibility compatibility;
    internal readonly OperationalLog log;
    internal readonly SessionAuthorityRegistry authority;
    internal readonly AuthorizedSessionProjector projector;

    /// <summary>Creates an engine host with cryptographically random session capabilities.</summary>
    public EngineHost(
        IGameFactory factory,
        ISessionCapabilityIssuer? capabilities = null,
        IVisibilityPolicy? visibility = null,
        ISessionStore? store = null,
        OperationalLog? log = null)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.visibility = visibility ?? new PermissiveVisibilityPolicy();
        this.store = store ?? new MemorySessionStore();
        this.log = log ?? OperationalLog.None;
        authority = new SessionAuthorityRegistry(
            capabilities ?? new CryptographicCapabilityIssuer());
        if (store is not null && factory is not IDurableGameFactory)
        {
            throw new ArgumentException(
                "persistent hosts require a factory with replay compatibility identities",
                nameof(factory));
        }

        compatibility = factory is IDurableGameFactory durable
            ? durable.Compatibility
            : TestCompatibility();
        projector = new AuthorizedSessionProjector(compatibility, this.ReplayOpen);
        this.Restore();
    }

    /// <inheritdoc />
    public EngineResponse Exchange(EngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var execution = new RequestExecution(
            log,
            request,
            authority.AuthorizedSeat(request),
            authority.CurrentRevision(request));
        EngineResponse response;

        try
        {
            if (request.Operation is EngineProtocol.Open or EngineProtocol.Attach && EntryReceipts.Valid(request.Entry))
            {
                int stripe = Convert.ToInt32(EntryReceipts.Verifier(request.Entry!)[..2], 16) % entryGates.Length;
                lock (entryGates[stripe]) response = HandleSerialized(request, execution);
            }
            else response = HandleSerialized(request, execution);
        }
        catch (PersistenceFailureException)
        {
            response = Failed(
                request,
                "save_failed",
                "the requested change was not committed and prior durable state remains authoritative");
        }
        catch (Exception)
        {
            // A socket cannot throw a domain exception into its caller. The
            // same conversion happens here, before either transport, so local
            // play and hosted play have the same observable failure path. A
            // stack trace is server state and never crosses the boundary.
            response = Failed(
                request,
                "engine_error",
                "the engine request failed without changing an existing session");
        }

        return execution.Complete(response);
    }

    private EngineResponse HandleSerialized(EngineRequest request, RequestExecution execution)
    {
        object? sessionGate = authority.SessionGate(request);
        if (sessionGate is null) return Handle(request, execution);
        lock (sessionGate) return Handle(request, execution);
    }

    private EngineResponse Handle(EngineRequest request, RequestExecution execution)
    {
        EngineResponse? envelopeFailure = ValidateEnvelope(request);
        if (envelopeFailure is not null) return envelopeFailure;

        if (request.Operation == EngineProtocol.Decks) return DeckRequests.Handle(request, factory);
        if (request.Deck is not null)
            return Failed(request, "invalid_request", "only deck queries accept an editable draft");

        if (request.Operation == EngineProtocol.Setup)
        {
            return Setup(request);
        }

        if (string.IsNullOrWhiteSpace(request.GameId))
        {
            return Failed(request, "invalid_request", "game_id is required");
        }

        if (request.GameId.Length > EngineProtocol.MaximumIdentifierLength)
        {
            return Failed(
                request, "invalid_request",
                $"game_id exceeds {EngineProtocol.MaximumIdentifierLength} characters");
        }

        EngineResponse? entryResponse = RecoverOrRejectEntry(request);
        if (entryResponse is not null) return entryResponse;

        return request.Operation switch
        {
            EngineProtocol.Open => this.Open(request, execution),
            EngineProtocol.Attach => this.Attach(request, execution),
            EngineProtocol.Sync => Sync(request),
            EngineProtocol.Resolve => this.Resolve(request, execution),
            EngineProtocol.Undo => this.MoveHistory(request, execution, undo: true),
            EngineProtocol.Redo => this.MoveHistory(request, execution, undo: false),
            EngineProtocol.Reorder => this.ReorderHistory(request, execution),
            EngineProtocol.Close => this.Close(request, execution),
            _ => Failed(
                request, "invalid_request",
                $"operation '{request.Operation}' is not supported"),
        };
    }

    private static EngineResponse? ValidateEnvelope(EngineRequest request)
    {
        if (request.Version != EngineProtocol.Version)
        {
            return Failed(
                request, "unsupported_version",
                $"protocol {request.Version} is not supported; expected {EngineProtocol.Version}");
        }

        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            return Failed(request, "invalid_request", "request_id is required");
        }

        if (request.RequestId.Length > EngineProtocol.MaximumIdentifierLength)
        {
            return Failed(
                request, "invalid_request",
                $"request_id exceeds {EngineProtocol.MaximumIdentifierLength} characters");
        }

        return null;
    }

    private EngineResponse? RecoverOrRejectEntry(EngineRequest request)
    {
        if (request.Operation is EngineProtocol.Open or EngineProtocol.Attach)
        {
            if (!EntryReceipts.Valid(request.Entry)) return Failed(request, "invalid_request", "entry requires a valid recovery claim");
            if (authority.RetiredEntry(request.Entry!)) return Failed(request, "session_not_found", "the entry authority has been retired");
            if (authority.TryEntry(request.Entry!, out var existing))
                return this.RecoverEntry(request, existing);
        }
        else if (request.Entry is not null) return Failed(request, "invalid_request", "only entry operations accept a recovery claim");

        return null;
    }

    private EngineResponse Setup(EngineRequest request)
    {
        if (!SetupRequest(request))
            return Failed(request, "invalid_request", "setup accepts only a request id");

        if (factory is not ISetupDiscovery discovery)
        {
            return Failed(
                request, "setup_unavailable",
                "setup choices are not available from this host");
        }

        SetupChoices choices = discovery.DiscoverSetup();
        return new EngineResponse(
            EngineProtocol.Version,
            request.RequestId,
            GameId: string.Empty,
            Capability: null,
            Prompt: null,
            Events: [],
            Setup: choices);
    }

    private static bool SetupRequest(EngineRequest request) => request is
    {
        GameId: "", Entry: null, Capability: null, Game: null, Decision: null,
        Viewer: null, ExpectedRevision: null, Cursor: null, Order: null,
    };

    private EngineResponse Sync(EngineRequest request)
    {
        if (request.Game is not null
            || request.Decision is not null
            || request.Viewer is not null
            || request.ExpectedRevision is not null
            || request.Cursor is not null
            || request.Order is not null)
        {
            return Failed(request, "invalid_request", "sync accepts only a session capability");
        }

        if (!authority.TrySession(request, out _, out var access))
        {
            return Failed(request, "session_not_found", "the session capability is not valid");
        }

        return AuthorizedSessionProjector.Succeeded(
            request,
            access.Session.Game,
            access.Session.Game.Pending,
            [],
            access.Scope,
            revision: access.Session.Revision,
            history: projector.History(access.Session, access.Scope));
    }

}
