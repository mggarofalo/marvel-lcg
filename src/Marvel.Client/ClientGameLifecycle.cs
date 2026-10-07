using Marvel.Decisions;
using Marvel.Server;

namespace Marvel.Client;

/// <summary>
/// Owns one displayed session's request admission, authoritative replacement and recovery.
/// These are client product choices; the tabletop rules do not define transport lifecycle.
/// </summary>
public sealed class ClientGameLifecycle
{
    private readonly object gate = new();
    private LocalGameClient? client;
    private ClientSession? session;
    private long generation;
    private bool busy;
    private ClientStartupError? uncertain;

    /// <summary>The latest complete authorized table, including its revision and history.</summary>
    public EngineResponse? CurrentGame { get; private set; }
    /// <summary>Persistent operational meaning, independent of any composition surface.</summary>
    public GameProgressPresentation? Progress { get; private set; }
    /// <summary>Whether a read can begin, including recovery of an uncertain mutation.</summary>
    public bool CanSynchronize => session is not null && !busy;
    /// <summary>Whether a new answer can begin on the current authoritative prompt.</summary>
    public bool CanResolve => CanSynchronize && Progress?.LocksDecisions == false;
    /// <summary>Whether setup discovery or entry is in progress.</summary>
    public bool EntryPending { get; private set; }

    /// <summary>Invalidates old requests before beginning a new setup or entry attempt.</summary>
    public long BeginEntry()
    {
        lock (gate)
        {
            Detach();
            EntryPending = true;
            return generation;
        }
    }

    /// <summary>Whether an asynchronous setup or entry result still belongs to this lifetime.</summary>
    public bool IsCurrentEntry(long ticket) => EntryPending && IsCurrentLifetime(ticket);

    /// <summary>Whether a continuation belongs to the active entry or table lifetime.</summary>
    public bool IsCurrentLifetime(long ticket) => ticket == generation;

    /// <summary>Ends setup discovery or a failed entry without installing a table.</summary>
    public bool FinishEntry(long ticket)
    {
        lock (gate)
        {
            if (!IsCurrentEntry(ticket)) return false;
            EntryPending = false;
            return true;
        }
    }

    /// <summary>Installs a validated entry only if no newer entry or detach superseded it.</summary>
    public bool Enter(long ticket, LocalGameClient connection, ClientEntryResult entry)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entry);
        lock (gate)
        {
            if (!IsCurrentEntry(ticket) || !entry.Succeeded) return false;
            client = connection;
            session = entry.Session!;
            CurrentGame = entry.Response!;
            Progress = GameProgressPresentation.FromResponse(CurrentGame);
            EntryPending = false;
            return true;
        }
    }

    /// <summary>Forgets session authority and invalidates every outstanding continuation.</summary>
    public void Detach()
    {
        lock (gate)
        {
            generation = checked(generation + 1);
            client = null;
            session = null;
            CurrentGame = null;
            Progress = null;
            uncertain = null;
            busy = false;
            EntryPending = false;
        }
    }

    /// <summary>Whether a history boundary is advertised and mutation is currently safe.</summary>
    public bool CanUndo(int cursor) => CanSynchronize && AllowsHistoryChange
        && CurrentGame?.History?.Undo.Contains(cursor) == true;

    private bool AllowsHistoryChange => uncertain is null && HistoryRequestAdmission.Allows(Progress);

    /// <summary>Submits once, preserving a proven not-sent draft and never retrying uncertainty.</summary>
    public Task<ClientLifecycleUpdate?> ResolveAsync(
        long revision, EngineDecision decision, CancellationToken cancellationToken = default) =>
        MutateAsync(revision, decision, cursor: null, cancellationToken);

    /// <summary>Submits one advertised undo without replaying an uncertain mutation.</summary>
    public Task<ClientLifecycleUpdate?> UndoAsync(
        int cursor, CancellationToken cancellationToken = default) =>
        MutateAsync(CurrentGame?.Revision ?? -1, decision: null, cursor, cancellationToken);

    private async Task<ClientLifecycleUpdate?> MutateAsync(
        long revision, EngineDecision? decision, int? cursor, CancellationToken cancellationToken)
    {
        LocalGameClient connection;
        ClientSession authority;
        long ticket;
        lock (gate)
        {
            if (CurrentGame?.Revision != revision
                || (cursor is { } target ? !CanUndo(target) : !CanResolve)) return null;
            if (cancellationToken.IsCancellationRequested)
                return CompleteMutation(new(null, Cancelled(), ClientMutationDisposition.NotSent), cursor.HasValue);
            connection = client!;
            authority = session!;
            ticket = generation;
            busy = true;
            Progress = cursor.HasValue ? GameProgressPresentation.Undoing() : GameProgressPresentation.Resolving();
        }
        ClientResolutionResult result;
        try
        {
            result = cursor is { } target
                ? await connection.UndoAsync(authority, target, cancellationToken)
                : await connection.ResolveAsync(authority, decision!, cancellationToken);
        }
        catch (Exception)
        {
            // Once dispatched, cancellation or an unexpected failure cannot prove non-delivery.
            result = new(null, new("transport_unavailable",
                "The response was interrupted. Synchronize the table before taking another action."),
                ClientMutationDisposition.Uncertain);
        }
        lock (gate)
        {
            if (ticket != generation) return null;
            busy = false;
            return CompleteMutation(result, cursor.HasValue);
        }
    }

    private ClientLifecycleUpdate CompleteMutation(ClientResolutionResult result, bool historyChange)
    {
        if (result.SessionDisposition == ClientSessionDisposition.Unavailable)
            return LoseSession(result.Error!);
        if (result.MutationDisposition == ClientMutationDisposition.NotSent)
            return CompleteNotSent(result, historyChange);
        if (result.Response is { } response)
        {
            CurrentGame = response;
            uncertain = null;
            Progress = result.Error is null ? GameProgressPresentation.FromResponse(response)
                : GameProgressPresentation.Recovered(response, result.Error);
            return new(response, Progress, ClientDraftDisposition.Replace, result.Error, result.Succeeded,
                result.MutationDisposition);
        }
        ClientStartupError error = result.Error ?? new("decision_unresolved",
            "The result could not be reconciled with the current table.");
        uncertain = result.MutationDisposition == ClientMutationDisposition.Rejected ? null : error;
        Progress = uncertain is null ? GameProgressPresentation.DecisionRejected(error)
            : GameProgressPresentation.Unconfirmed(error);
        return new(null, Progress, ClientDraftDisposition.Preserve, error, MutationDisposition: result.MutationDisposition);
    }

    private ClientLifecycleUpdate CompleteNotSent(ClientResolutionResult result, bool historyChange)
    {
        Progress = historyChange ? GameProgressPresentation.UndoNotSent(result.Error ?? Cancelled())
            : GameProgressPresentation.DecisionNotSent(result.Error ?? Cancelled());
        return new(null, Progress, historyChange ? ClientDraftDisposition.Preserve : ClientDraftDisposition.Retry,
            result.Error, MutationDisposition: result.MutationDisposition);
    }

    /// <summary>Reads authority once; successful replacement always invalidates the old draft.</summary>
    public async Task<ClientLifecycleUpdate?> SynchronizeAsync(
        bool hasDraft = false, CancellationToken cancellationToken = default)
    {
        LocalGameClient connection;
        ClientSession authority;
        GameProgressPresentation prior;
        long ticket;
        lock (gate)
        {
            if (!CanSynchronize) return null;
            connection = client!;
            authority = session!;
            prior = Progress!;
            ticket = generation;
            busy = true;
            Progress = GameProgressPresentation.Synchronizing();
        }
        ClientSynchronizationResult result;
        try { result = await connection.SynchronizeAsync(authority, cancellationToken); }
        catch (Exception)
        {
            result = new(null, new("synchronization_failed",
                "The current table could not be read. Try reconnecting again."), ClientSessionDisposition.Active);
        }
        lock (gate)
        {
            if (ticket != generation) return null;
            busy = false;
            if (result.SessionDisposition == ClientSessionDisposition.Unavailable)
                return LoseSession(result.Error!);
            if (result.Response is { } response)
            {
                CurrentGame = response;
                uncertain = null;
                Progress = GameProgressPresentation.FromSynchronization(response, prior);
                if (hasDraft && Progress.OperationalLock is null)
                    Progress = GameProgressPresentation.Refreshed(response, draftCleared: true);
                return new(response, Progress, ClientDraftDisposition.Replace);
            }
            Progress = uncertain is not null ? UnconfirmedProgress(uncertain, prior)
                : GameProgressPresentation.SynchronizationUnavailable(result.Error!,
                    prior.LocksDecisions, prior.OperationalLock);
            return new(null, Progress, ClientDraftDisposition.Preserve, result.Error);
        }
    }

    /// <summary>Locks interaction when an accepted table could not be presented.</summary>
    public void PresentationFailed()
    {
        lock (gate)
        {
            if (session is null) return;
            uncertain = new("display_failed", "The current table could not be displayed. Synchronize before continuing.");
            Progress = UnconfirmedProgress(uncertain, Progress);
        }
    }

    private static GameProgressPresentation UnconfirmedProgress(
        ClientStartupError error, GameProgressPresentation? prior) =>
        GameProgressPresentation.Unconfirmed(error) with { OperationalLock = prior?.OperationalLock };

    private ClientLifecycleUpdate LoseSession(ClientStartupError error)
    {
        Detach();
        Progress = GameProgressPresentation.Unavailable(error);
        return new(null, Progress, ClientDraftDisposition.Clear, error);
    }

    private static ClientStartupError Cancelled() => new("decision_not_sent",
        "The request was cancelled before it was sent.");
}
