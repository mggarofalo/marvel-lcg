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

/// <summary>A single-threaded collection of deterministic engine sessions.</summary>
/// <remarks>
/// The host is deliberately synchronous and has no lock. Socket I/O finishes
/// before a request reaches this type, and the standalone server handles one
/// request at a time. Threading or async must never become a path into game
/// state; see <c>AGENTS.md</c>, “Determinism is load-bearing”.
/// Operation-specific validation stays here. <see cref="SessionAuthorityRegistry"/>,
/// <see cref="SessionTransaction"/>, <see cref="AuthorizedSessionProjector"/>,
/// and <see cref="RequestExecution"/> own authorization, durable publication,
/// visibility-safe projection, and request telemetry respectively.
/// </remarks>
public sealed class EngineHost : IEngineEndpoint
{
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
            response = Handle(request, execution);
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

    private EngineResponse Handle(EngineRequest request, RequestExecution execution)
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

        return request.Operation switch
        {
            EngineProtocol.Open => Open(request, execution),
            EngineProtocol.Attach => Attach(request, execution),
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

    private EngineResponse Setup(EngineRequest request)
    {
        if (request.GameId is not ""
            || request.Capability is not null
            || request.Game is not null
            || request.Decision is not null
            || request.Viewer is not null
            || request.ExpectedRevision is not null
            || request.Cursor is not null
            || request.Order is not null)
        {
            return Failed(
                request, "invalid_request",
                "setup accepts only a request id");
        }

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

    private EngineResponse Open(EngineRequest request, RequestExecution execution)
    {
        EngineResponse? invalid = OpenRequestError(request);
        if (invalid is not null) return invalid;

        if (!TryAuthorizeOpen(request, out ViewScope scope,
                out IReadOnlyList<SeatScope> additionalScopes, out EngineResponse? failure))
            return failure!;

        var opened = factory.Create(request.Game!);
        string storageId = NewStorageId();
        SessionSave save = SessionSave.Open(
            compatibility,
            storageId,
            request.GameId,
            ToSessionSetup(request.Game!),
            opened.Game,
            opened.SetupEvents);
        var session = new HostedSession(request.GameId, opened.Game, save);
        string capability = authority.Issue();
        var reserved = new HashSet<string>(StringComparer.Ordinal) { capability };
        var issuedInvitations = additionalScopes
            .Select(grant =>
            {
                string token = authority.Issue(reserved);
                reserved.Add(token);
                return (grant, token);
            })
            .ToList();
        EngineResponse response = AuthorizedSessionProjector.Succeeded(
            request,
            opened.Game,
            opened.Game.Pending,
            opened.SetupEvents,
            scope,
            capability,
            issuedInvitations.Select(pair =>
                new SeatInvitation(pair.grant.Seat, pair.token)).ToList(),
            history: projector.History(save, scope, opened.Game));
        List<StoredAuthority> proposedAuthorities =
            SessionAuthorityRegistry.OpenAuthorities(
                capability,
                scope,
                request.Game!.Heroes.Count,
                issuedInvitations.Select(pair => (pair.grant, pair.token)).ToList());
        execution.ObservePersistence(() =>
            store.Commit(new StoredSession(save, proposedAuthorities)));
        authority.PublishOpen(
            session,
            capability,
            scope,
            issuedInvitations.Select(pair => (pair.grant, pair.token)).ToList());

        return response;
    }

    private static EngineResponse? OpenRequestError(EngineRequest request)
    {
        if (request.Game is null || request.Decision is not null
            || request.ExpectedRevision is not null || request.Cursor is not null
            || request.Order is not null)
            return Failed(request, "invalid_request",
                "open requires game and does not accept decision");
        if (request.Capability is not null)
            return Failed(request, "invalid_request", "open does not accept capability");
        if (string.IsNullOrWhiteSpace(request.Game.Scenario))
            return Failed(request, "invalid_request", "a game requires a scenario");
        if (request.Game.Heroes is not { Count: > 0 }
            || request.Game.Heroes.Any(string.IsNullOrWhiteSpace))
            return Failed(request, "invalid_request", "a game requires at least one hero");
        return null;
    }

    private bool TryAuthorizeOpen(
        EngineRequest request, out ViewScope scope,
        out IReadOnlyList<SeatScope> additionalScopes, out EngineResponse? failure)
    {
        try
        {
            scope = visibility.Authorize(request.Viewer, request.Game!.Heroes.Count)
                ?? throw new ArgumentException("visibility policy returned no primary scope");
            additionalScopes = visibility.AdditionalScopes(
                request.Viewer, request.Game.Heroes.Count)?.ToList()
                ?? throw new ArgumentException(
                    "visibility policy returned no additional-scope collection");
            ValidateAdditionalScopes(scope, additionalScopes, request.Game.Heroes.Count);
            failure = null;
            return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        {
            scope = null!;
            additionalScopes = [];
            failure = Failed(request, "invalid_request", error.Message);
            return false;
        }
    }

    private static void ValidateAdditionalScopes(
        ViewScope primary, IReadOnlyList<SeatScope>? grants, int players)
    {
        if (grants is null)
        {
            throw new ArgumentException("visibility policy returned no additional-scope collection");
        }

        var seats = new HashSet<int>();
        foreach (SeatScope? grant in grants)
        {
            if (grant is null)
            {
                throw new ArgumentException("visibility policy returned an empty seat grant");
            }

            if (grant.Seat < 0 || grant.Seat >= players)
            {
                throw new ArgumentException(
                    $"visibility policy seat {grant.Seat} is outside this game");
            }

            if (!seats.Add(grant.Seat))
            {
                throw new ArgumentException(
                    $"visibility policy returned seat {grant.Seat} more than once");
            }

            if (primary.Includes(grant.Seat))
            {
                throw new ArgumentException(
                    $"visibility policy returned primary seat {grant.Seat} as an additional grant");
            }

            if (grant.Scope is null || !grant.Scope.IsExactly(grant.Seat))
            {
                throw new ArgumentException(
                    $"visibility policy grant for seat {grant.Seat} must authorize exactly that seat");
            }
        }
    }

    private EngineResponse Attach(EngineRequest request, RequestExecution execution)
    {
        if (request.Game is not null
            || request.Decision is not null
            || request.Viewer is not null
            || request.ExpectedRevision is not null
            || request.Cursor is not null
            || request.Order is not null)
        {
            return Failed(
                request, "invalid_request",
                "attach accepts only a server-issued invitation");
        }

        string invitation = request.Capability ?? string.Empty;
        if (!authority.TryInvitation(
                invitation, request.GameId, out string invitationVerifier, out var pending))
        {
            return Failed(request, "session_not_found", "the seat invitation is not valid");
        }

        string capability = authority.Issue();
        EngineResponse response = AuthorizedSessionProjector.Succeeded(
            request,
            pending.Session.Game,
            pending.Session.Game.Pending,
            [],
            pending.Scope,
            capability,
            revision: pending.Session.Revision,
            history: projector.History(
                pending.Session.Save, pending.Scope, pending.Session.Game));
        List<StoredAuthority> authorities = authority.AttachmentAuthorities(
            pending.Session, invitationVerifier, capability, pending.Scope);
        SessionSave stamped = this.Stamp(pending.Session.Save);
        execution.ObservePersistence(() =>
            store.Commit(new StoredSession(stamped, authorities)));
        pending.Session.Publish(stamped);
        authority.PublishAttachment(invitationVerifier, capability, pending);
        return response;
    }

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
            history: projector.History(
                access.Session.Save, access.Scope, access.Session.Game));
    }

}
