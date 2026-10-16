using Marvel.Protocol;
using Marvel.Session;
using Marvel.View;
using static Marvel.Server.EngineHostLifecycle;

namespace Marvel.Server;

/// <summary>Admits initial session authority and redeems scoped invitations durably.</summary>
internal static class EngineHostEntry
{
    internal static EngineResponse Open(this EngineHost host, EngineRequest request, RequestExecution execution)
    {
        EngineResponse? invalid = OpenRequestError(request);
        if (invalid is not null) return invalid;

        if (!TryAuthorizeOpen(host, request, out ViewScope scope,
                out IReadOnlyList<SeatScope> additionalScopes, out EngineResponse? failure))
            return failure!;

        var opened = host.factory.Create(request.Game!);
        string storageId = NewStorageId();
        SessionSave save = SessionSave.Open(
            host.compatibility,
            storageId,
            request.GameId,
            ToSessionSetup(request.Game!),
            opened.Game,
            opened.SetupEvents);
        var session = new HostedSession(request.GameId, opened.Game, save);
        string capability = host.authority.Issue();
        var reserved = new HashSet<string>(StringComparer.Ordinal) { capability };
        var issuedInvitations = additionalScopes
            .Select(grant =>
            {
                string token = host.authority.Issue(reserved);
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
            history: host.projector.History(session, scope));
        List<StoredAuthority> proposedAuthorities =
            SessionAuthorityRegistry.OpenAuthorities(
                capability,
                scope,
                request.Game!.Heroes.Count,
                issuedInvitations.Select(pair => (pair.grant, pair.token)).ToList());
        StoredEntryReceipt receipt = EntryReceipts.Protect(request, response);
        proposedAuthorities[0] = proposedAuthorities[0] with { Entry = receipt };
        execution.ObservePersistence(() =>
            host.store.Commit(new StoredSession(save, proposedAuthorities)));
        host.authority.PublishOpen(
            session,
            capability,
            scope,
            issuedInvitations.Select(pair => (pair.grant, pair.token)).ToList(), receipt);

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

    private static bool TryAuthorizeOpen(
        EngineHost host, EngineRequest request, out ViewScope scope,
        out IReadOnlyList<SeatScope> additionalScopes, out EngineResponse? failure)
    {
        try
        {
            scope = host.visibility.Authorize(request.Viewer, request.Game!.Heroes.Count)
                ?? throw new ArgumentException("visibility policy returned no primary scope");
            additionalScopes = host.visibility.AdditionalScopes(
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

    internal static EngineResponse Attach(this EngineHost host, EngineRequest request, RequestExecution execution)
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
        if (!host.authority.TryInvitation(
                invitation, request.GameId, out string invitationVerifier, out var pending))
        {
            return Failed(request, "session_not_found", "the seat invitation is not valid");
        }

        string capability = host.authority.Issue();
        EngineResponse response = AuthorizedSessionProjector.Succeeded(
            request,
            pending.Session.Game,
            pending.Session.Game.Pending,
            [],
            pending.Scope,
            capability,
            revision: pending.Session.Revision,
            history: host.projector.History(pending.Session, pending.Scope));
        List<StoredAuthority> authorities = host.authority.AttachmentAuthorities(
            pending.Session, invitationVerifier, capability, pending.Scope);
        StoredEntryReceipt receipt = EntryReceipts.Protect(request, response);
        int authorityIndex = authorities.FindIndex(item => item.Verifier == SessionAuthorityRegistry.Verifier(capability));
        authorities[authorityIndex] = authorities[authorityIndex] with { Entry = receipt };
        SessionSave stamped = host.Stamp(pending.Session.Save);
        execution.ObservePersistence(() =>
            host.store.Commit(new StoredSession(stamped, authorities)));
        pending.Session.Publish(stamped);
        host.authority.PublishAttachment(invitationVerifier, capability, pending, receipt);
        return response;
    }

}
