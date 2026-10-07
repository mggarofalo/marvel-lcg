using Marvel.Server;

namespace Marvel.Client;

/// <summary>Explains authoritative history availability within the current request lifecycle.</summary>
public static class HistoryUndoPresentation
{
    /// <summary>Describes why the latest action can or cannot be undone.</summary>
    public static string Describe(bool allowed, HistoryDescriptor? history, GameProgressPresentation? progress)
    {
        if (allowed) return "Undo the latest completed action.";
        if (progress is not null && !HistoryRequestAdmission.Allows(progress))
            return progress.Description;
        return history?.UndoStatus switch
        {
            HistoryUndoStatus.NoHistory => "There is no completed action to undo.",
            HistoryUndoStatus.ActionInProgress => "Finish the current action before undoing it.",
            HistoryUndoStatus.ProtectedHistory =>
                "This action cannot be undone because it exposed information or used hidden randomness.",
            HistoryUndoStatus.OtherPlayer => "This action includes another player's decisions and cannot be undone by you.",
            _ => "The server does not offer undo for the latest action.",
        };
    }
}
