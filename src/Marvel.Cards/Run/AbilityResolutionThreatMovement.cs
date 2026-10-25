using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
namespace Marvel.Cards.Run;

internal static class AbilityResolutionThreatMovement
{
    internal static void MoveThreat(this AbilityResolutionExecution execution,
        AbilityEffect.MoveThreat move, AbilityResolutionState cast)
    {
        var from = execution.Find(move.From, cast);
        var to = execution.Find(move.To, cast);
        cast.ThreatMoveRequested = Math.Max(0, execution.Amount(move.Amount, cast));
        cast.Results["threatMoved"] = 0;
        cast.Results["threatRemovedByMove"] = 0;
        cast.Results["destinationForcedResponseTriggered"] = 0;
        cast.PendingThreatMove = ThreatMovement.Schedule(cast.World,
            new(from, to, cast.ThreatMoveRequested,
                cast.Source, cast.Trigger, cast.Player));
        execution.SuspendAfterProcedure(move, cast);
    }
    internal static AbilityStructuralOutcome Complete(ThreatMoveReceipt receipt, long requested) =>
        requested <= 0 || receipt.Removed <= 0 && receipt.Placed <= 0
            ? AbilityStructuralOutcome.None
            : receipt.Removed >= requested && receipt.Placed >= requested
                ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.Partial;
}
