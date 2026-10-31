namespace Marvel.Cards.Run;

/// <summary>Settles committed procedure receipts before structural execution continues.</summary>
internal static class AbilityResumedEffectCompletion
{
    internal static void Apply(AbilityResolutionState cast, ContinueAfterResumedNode completed)
    {
        RestoreOutcome(cast, completed.State);
        cast.PendingThreatMove = null;
        cast.PendingCompletion = null;
        cast.PendingDamageCompletion = null;
        if (cast.CompletedOutcome is AbilityStructuralOutcome.Full or AbilityStructuralOutcome.Partial
            || completed.EffectApplied && completed.State.Completion is null
                && completed.State.DamageCompletion is null && completed.State.ThreatMove is null)
            cast.ResolveEffect();
    }

    private static void RestoreOutcome(AbilityResolutionState cast, AbilityContinuationState state)
    {
        if (state.DamageCompletion is { } damageReceipt)
        {
            cast.DamageDefeats = damageReceipt.Defeats;
            damageReceipt.RecordExcessDealt(cast.Results);
        }
        cast.CompletedOutcome = state.DamageCompletion is { } damage ? damage.Complete()
            : state.Completion is { } receipt
            ? AbilityCompletion.Amount(receipt.Applied, receipt.Requested)
            : state.CompletedOutcome ?? cast.CompletedOutcome;
        if (state.ThreatMove is { } move)
        {
            cast.Results["threatMoved"] = move.Placed;
            cast.Results["threatRemovedByMove"] = move.Removed;
            cast.Results["destinationForcedResponseTriggered"] = move.DestinationForcedResponseTriggered ? 1 : 0;
            cast.CompletedOutcome = AbilityResolutionThreatMovement.Complete(move, state.ThreatMoveRequested);
        }
    }
}
