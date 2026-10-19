using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Client-owned presentation continuity; never retains an answer across prompts.</summary>
internal sealed class LocalDecisionContinuity
{
    internal bool Active { get; private set; }
    internal bool Committed { get; private set; }
    internal bool Awaiting { get; private set; }
    internal long? AcceptedRevision { get; private set; }
    internal int Source { get; private set; }
    internal int Seat { get; private set; }
    internal long Revision { get; private set; }
    internal string Origin { get; private set; } = string.Empty;

    internal void Begin(int source, int seat, long revision, string origin)
    {
        Active = true;
        Committed = false;
        Awaiting = false;
        AcceptedRevision = null;
        Source = source;
        Seat = seat;
        Revision = revision;
        Origin = origin;
    }

    internal void Submit() { if (Active) Awaiting = true; }
    internal void Accept(long revision)
    { if (Awaiting && revision == Revision + 1) AcceptedRevision = revision; }
    internal void Reject() { Awaiting = false; AcceptedRevision = null; }
    internal void Clear() { Active = false; Reject(); }

    internal bool Refresh(int? player, PublicDecisionKind? kind, long revision)
    {
        if (!Active) return false;
        if (revision == Revision && player == Seat) return true;
        bool continuation = Awaiting && AcceptedRevision == revision && revision == Revision + 1 && player == Seat
            && kind is not null and not PublicDecisionKind.PlayerAction;
        Awaiting = false;
        Committed = AcceptedRevision == revision;
        AcceptedRevision = null;
        Revision = revision;
        if (!continuation) Clear();
        return continuation;
    }
}
