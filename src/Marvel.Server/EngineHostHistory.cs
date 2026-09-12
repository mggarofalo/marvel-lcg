using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.View;
using System.Diagnostics;
using System.Security.Cryptography;

using static Marvel.Server.EngineHostResolution;
using static Marvel.Server.EngineHostHistory;
using static Marvel.Server.EngineHostLifecycle;

namespace Marvel.Server;

/// <summary>Owns history operations for an engine host.</summary>
internal static class EngineHostHistory
{
    internal static EngineResponse MoveHistory(this EngineHost host,
        EngineRequest request, RequestExecution execution, bool undo)
    {
        string operation = undo ? EngineProtocol.Undo : EngineProtocol.Redo;
        if (!ValidMoveRequest(request))
        {
            return Failed(
                request,
                "invalid_request",
                $"{operation} requires an expected revision and history cursor");
        }

        if (!host.authority.TrySession(request, out _, out var access))
        {
            return Failed(request, "session_not_found", "the session capability is not valid");
        }

        SessionSave save = access.Session.Save;
        int target = request.Cursor!.Value;
        EngineResponse? unavailable = MoveUnavailable(
            request, save, access.Scope, target, undo, operation);
        if (unavailable is not null) return unavailable;

        try
        {
            Game candidate = execution.ObserveReplay(() =>
                SessionReplay.VerifyAtCursor(save, host.compatibility, host.ReplayOpen, target));
            SessionSave proposed = save with
            {
                Compatibility = host.compatibility,
                Revision = save.Revision + 1,
                Cursor = target,
                CurrentPrompt = candidate.Pending is null
                    ? null
                    : PromptRecord.From(candidate.Pending),
            };
            Game verified = execution.ObserveReplay(() =>
                SessionReplay.Verify(proposed, host.compatibility, host.ReplayOpen));
            var transaction = new SessionTransaction(verified, proposed);
            return transaction.CommitAndPublish(
                access.Session,
                host.authority.Snapshot(access.Session),
                stored => execution.ObservePersistence(() => host.store.Commit(stored)),
                (published, committed) => AuthorizedSessionProjector.Succeeded(
                    request,
                    published,
                    published.Pending,
                    [],
                    access.Scope,
                    revision: committed.Revision,
                    history: host.projector.History(committed, access.Scope, published)));
        }
        catch (Exception failure)
        {
            if (!HistoryFailure(failure)) throw;
            if (failure is ReplayDivergenceException)
            {
                execution.MarkReplayDiverged();
            }
            return Failed(
                request,
                "history_failed",
                "history replay was not committed and the prior game remains authoritative");
        }
    }

    private static bool ValidMoveRequest(EngineRequest request) =>
        request.Game is null && request.Decision is null && request.Viewer is null
        && request.ExpectedRevision is >= 0 && request.Cursor is >= 0
        && request.Order is null;

    private static bool ValidDirection(int target, SessionSave save, bool undo) =>
        target <= save.Units.Count
        && (undo ? target < save.Cursor : target > save.Cursor);

    private static EngineResponse? MoveUnavailable(
        EngineRequest request, SessionSave save, ViewScope scope,
        int target, bool undo, string operation)
    {
        if (request.ExpectedRevision != save.Revision)
            return Failed(request, "stale_history",
                "the history command was composed for an earlier table revision");
        if (!ValidDirection(target, save, undo))
            return Failed(request, "history_direction",
                $"{operation} cursor is not an available retained boundary");
        if (save.Units.Any(unit => unit.Status != "complete"))
            return Failed(request, "history_open",
                "history cannot change while an operation has dependent decisions pending");
        int first = Math.Min(target, save.Cursor);
        int count = Math.Abs(target - save.Cursor);
        IReadOnlyList<JournalUnit> affected = save.Units.Skip(first).Take(count).ToList();
        if (!AuthorizedSessionProjector.EditableBy(affected, scope))
            return Failed(request, "history_authority",
                "this capability cannot revise history submitted by another seat");
        return target < save.EditFrontier
            ? Failed(request, "history_frontier",
                "new information makes that earlier history boundary unavailable") : null;
    }

    internal static EngineResponse ReorderHistory(this EngineHost host,
        EngineRequest request, RequestExecution execution)
    {
        if (!ValidReorderRequest(request))
        {
            return Failed(
                request,
                "invalid_request",
                "reorder requires an expected revision and at least two unit positions");
        }

        if (!host.authority.TrySession(request, out _, out var access))
        {
            return Failed(request, "session_not_found", "the session capability is not valid");
        }

        SessionSave save = access.Session.Save;
        int[] order = [.. request.Order!];
        EngineResponse? invalidOrder = ReorderShapeUnavailable(
            request, save, order, out int first, out int last);
        if (invalidOrder is not null) return invalidOrder;
        List<JournalUnit> affected = save.Units.Skip(first).Take(order.Length).ToList();
        JournalUnit position = affected[0];
        EngineResponse? unavailable = ReorderUnavailable(
            request, save, access.Scope, order, first, last, affected, position);
        if (unavailable is not null) return unavailable;

        try
        {
            int[] sourceOrder =
            [
                .. Enumerable.Range(0, first),
                .. order,
                .. Enumerable.Range(last + 1, save.Cursor - last - 1),
            ];
            RewrittenTrace trace = SessionHistoryRewrite.Rewrite(
                save, host.compatibility, host.ReplayOpen, sourceOrder);
            SessionSave proposed = save with
            {
                Compatibility = host.compatibility,
                Revision = save.Revision + 1,
                Cursor = trace.Units.Count,
                EditFrontier = trace.EditFrontier,
                CurrentPrompt = trace.Game.Pending is null
                    ? null
                    : PromptRecord.From(trace.Game.Pending),
                Units = trace.Units,
            };
            Game verified = execution.ObserveReplay(() =>
                SessionReplay.Verify(proposed, host.compatibility, host.ReplayOpen));
            var transaction = new SessionTransaction(verified, proposed);
            return transaction.CommitAndPublish(
                access.Session,
                host.authority.Snapshot(access.Session),
                stored => execution.ObservePersistence(() => host.store.Commit(stored)),
                (published, committed) => AuthorizedSessionProjector.Succeeded(
                    request,
                    published,
                    published.Pending,
                    [],
                    access.Scope,
                    revision: committed.Revision,
                    history: host.projector.History(committed, access.Scope, published)));
        }
        catch (Exception failure)
        {
            if (!HistoryFailure(failure)) throw;
            return Failed(
                request,
                "reorder_failed",
                "the rewritten trace was not committed and the prior game remains authoritative");
        }
    }

    private static bool ValidReorderRequest(EngineRequest request) =>
        request.Game is null && request.Decision is null && request.Viewer is null
        && request.ExpectedRevision is >= 0 && request.Cursor is null
        && request.Order is { Count: >= 2 };

    private static bool ValidOrderIndexes(int[] order, int cursor) =>
        order.All(index => index >= 0 && index < cursor)
        && order.Distinct().Count() == order.Length;

    private static bool ChangesContiguousRange(int[] order, int first, int last) =>
        last - first + 1 == order.Length
        && !order.SequenceEqual(Enumerable.Range(first, order.Length));

    private static bool SameTurnAction(JournalUnit unit, JournalUnit position) =>
        unit.Role == "turn_action" && unit.ActiveSeat == position.ActiveSeat
        && unit.Round == position.Round
        && string.Equals(unit.Phase, position.Phase, StringComparison.Ordinal);

    private static EngineResponse? ReorderUnavailable(
        EngineRequest request, SessionSave save, ViewScope scope, int[] order,
        int first, int last, List<JournalUnit> affected, JournalUnit position)
    {
        if (save.Units.Any(unit => unit.Status != "complete"))
            return Failed(request, "history_open",
                "history cannot change while an operation has dependent decisions pending");
        if (!AuthorizedSessionProjector.EditableBy(affected, scope))
            return Failed(request, "history_authority",
                "this capability cannot revise history submitted by another seat");
        if (first < save.EditFrontier)
            return Failed(request, "history_frontier",
                "new information makes that history range unavailable");
        return affected.Any(unit => !SameTurnAction(unit, position))
            ? Failed(request, "reorder_kind",
                "only action units from one active-player turn can be reordered") : null;
    }

    private static EngineResponse? ReorderShapeUnavailable(
        EngineRequest request, SessionSave save, int[] order, out int first, out int last)
    {
        first = 0;
        last = 0;
        if (request.ExpectedRevision != save.Revision)
            return Failed(request, "stale_history",
                "the history command was composed for an earlier table revision");
        if (!ValidOrderIndexes(order, save.Cursor))
            return Failed(request, "reorder_shape",
                "reorder positions must name distinct active history units");
        first = order.Min();
        last = order.Max();
        return !ChangesContiguousRange(order, first, last)
            ? Failed(request, "reorder_shape",
                "reorder must change one contiguous range of history units") : null;
    }

    private static bool HistoryFailure(Exception failure) => failure is IOException
        or UnauthorizedAccessException or SessionSaveException or ReplayDivergenceException;

}
