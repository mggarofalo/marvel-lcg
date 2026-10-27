using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
namespace Marvel.Cards.Run;

internal static class AbilityResolutionThreatMovement
{
    internal static void MoveThreat(this AbilityResolutionExecution execution,
        AbilityEffect.MoveThreat move, AbilityResolutionState cast)
    {
        var pairs = AbilityThreatMoveChoice.Pairs(execution.Every(move.From, cast),
            execution.Every(move.To, cast), cast.World,
            move.All ? 1 : execution.Amount(move.Amount, cast), cast.Source, cast.Player);
        if (pairs.Length > 1)
        {
            cast.ThreatMovePairs = pairs;
            execution.SuspendForChoice(move, cast);
            return;
        }
        Schedule(execution, move, cast, pairs.SingleOrDefault());
    }

    internal static void Schedule(AbilityResolutionExecution execution,
        AbilityEffect.MoveThreat move, AbilityResolutionState cast, AbilityThreatMovePair? pair)
    {
        var from = pair?.From.Card;
        var to = pair?.To.Card;
        cast.ThreatMoveRequested = move.All ? from?.Tokens.GetValueOrDefault("k_threat") ?? 0
            : Math.Max(0, execution.Amount(move.Amount, cast));
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
