using Marvel.Session;
using Marvel.View;

namespace Marvel.Server;

/// <summary>Projects authorized edit boundaries without disclosing frontier signals.</summary>
internal static class SessionHistoryEditProjection
{
    internal static HistoryDescriptor Describe(SessionSave save, ViewScope scope,
        IReadOnlyList<HistoryEntryDescriptor> entries)
    {
        bool actionOpen = save.Units.Any(unit => unit.Status != "complete");
        if (actionOpen)
        {
            return new HistoryDescriptor(save.Cursor, [], [], entries, ActionOpen: true)
            { UndoStatus = HistoryUndoStatus.ActionInProgress };
        }

        int[] undo = Enumerable.Range(save.EditFrontier, save.Cursor - save.EditFrontier)
            .Where(target => AuthorizedSessionProjector.EditableBy(
                save.Units.Skip(target).Take(save.Cursor - target), scope))
            .ToArray();
        int[] redo = Enumerable.Range(save.Cursor + 1, save.Units.Count - save.Cursor)
            .Where(target => AuthorizedSessionProjector.EditableBy(
                save.Units.Skip(save.Cursor).Take(target - save.Cursor), scope))
            .ToArray();
        return new HistoryDescriptor(save.Cursor, undo, redo, entries, ActionOpen: false)
        { UndoStatus = LatestStatus(save, scope, undo) };
    }

    private static HistoryUndoStatus LatestStatus(SessionSave save, ViewScope scope, int[] undo)
    {
        if (save.Cursor == 0) return HistoryUndoStatus.NoHistory;
        if (undo.Contains(save.Cursor - 1)) return HistoryUndoStatus.Available;
        // Authority is checked first so another seat's protected operation is not classified.
        if (!AuthorizedSessionProjector.EditableBy([save.Units[save.Cursor - 1]], scope))
            return HistoryUndoStatus.OtherPlayer;
        return HistoryUndoStatus.ProtectedHistory;
    }

}
