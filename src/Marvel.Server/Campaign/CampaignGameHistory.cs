using Marvel.Protocol;
using Marvel.Rules.Campaign;
namespace Marvel.Server;

/// <summary>Restricts history offers to decisions after the latest committed campaign consequence.</summary>
internal static class CampaignGameHistory
{
    internal static EngineResponse Project(StoredCampaign campaign, EngineResponse response)
    {
        if (response.History is not { } history) return response;
        var attempt = campaign.Save.Attempts.Single(item => item.Session.Label == response.GameId);
        int ordinal = attempt.Setup.Campaign!.Attempt;
        var proof = campaign.Save.Journal.Last(entry => entry.Proof?.Setup.Campaign?.Attempt == ordinal).Proof!;
        bool sealedAttempt = campaign.Save.State.Phase != CampaignPhase.Playing || campaign.Save.State.Attempt != ordinal;
        int[] undo = sealedAttempt ? [] : history.Undo.Where(cursor => cursor >= proof.Cursor).ToArray();
        return response with { History = history with { Undo = undo,
            Redo = sealedAttempt ? [] : history.Redo,
            UndoStatus = undo.Length == 0 && history.Undo.Count > 0 ? HistoryUndoStatus.ProtectedHistory : history.UndoStatus } };
    }

    internal static bool Allowed(StoredCampaign campaign, EngineRequest request)
    {
        if (request.Operation is not (EngineProtocol.Undo or EngineProtocol.Redo or EngineProtocol.Reorder)) return true;
        var attempt = campaign.Save.Attempts.Single(item => item.Session.Label == request.GameId);
        if (campaign.Save.State.Phase != CampaignPhase.Playing || campaign.Save.State.Attempt != attempt.Setup.Campaign!.Attempt) return false;
        var proof = campaign.Save.Journal.Last(entry => entry.Proof?.Setup.Campaign?.Attempt == campaign.Save.State.Attempt).Proof!;
        if (request.Cursor is { } cursor) return cursor >= proof.Cursor;
        if (request.Order is not { } order) return false;
        return order.Take(proof.Cursor).SequenceEqual(Enumerable.Range(0, proof.Cursor));
    }
}
