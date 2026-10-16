using Marvel.Protocol;
using static Marvel.Server.EngineHostLifecycle;

namespace Marvel.Server;

/// <summary>Recovers issued authority without executing an entry operation twice.</summary>
internal static class EngineHostEntryRecovery
{
    public static EngineResponse RecoverEntry(this EngineHost host, EngineRequest request, SessionAccess access)
    {
        if (access.Revoked) return Failed(request, "session_not_found", "the entry authority has been revoked");
        StoredEntryReceipt receipt = access.Entry!;
        if (receipt.Fingerprint != EntryReceipts.Fingerprint(request))
            return Failed(request, "entry_conflict", "the recovery claim belongs to a different entry request");
        EngineResponse issued = EntryReceipts.Recover(request, receipt);
        // A consumed or revoked invitation is never reissued by recovery of its owner's open request.
        var invitations = issued.Invitations?.Where(invitation => host.authority.TryInvitation(
            invitation.Invitation, request.GameId, out _, out _)).ToArray();
        return AuthorizedSessionProjector.Succeeded(request, access.Session.Game,
            access.Session.Game.Pending, [], access.Scope, issued.Capability, invitations,
            access.Session.Revision, host.projector.History(access.Session, access.Scope));
    }
}
