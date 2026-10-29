using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Completes an accepted payment choice without replaying the player's answer.</summary>
internal sealed class AbilityChoicePayment(ResourcePaymentReceipt payment, Card? ally = null,
    int? originIncarnation = null, int? originArea = null)
    : AbilityPaymentPlan
{
    private readonly int incarnation = originIncarnation ?? ally?.Incarnation ?? -1;
    private readonly int area = originArea ?? ally?.Area.Id ?? -1;
    private bool windowOpened;
    private bool entryScheduled;
    private EntryPayment? entryPayment;

    internal override bool Commit(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        if (!payment.IsComplete)
            throw new InvalidOperationException("payment choice resumed before its resource costs completed");
        if (entryScheduled)
        {
            if (entryPayment!.MatchesPending(ally!, cast.Player))
                throw new InvalidOperationException("paid ally entry has not completed");
            cast.ResolveEffect();
            return true;
        }
        CaptureEntryPayment(cast);
        if (!windowOpened)
        {
            windowOpened = true;
            if (ResourceSpentWindow.Open(cast.World, payment.SpentCards, cast.Player))
            {
                cast.Suspend();
                return false;
            }
        }
        return ally is null ? Finish(cast) : ScheduleEntry(execution, cast);
    }

    private void CaptureEntryPayment(AbilityResolutionState cast)
    {
        if (ally is null) return;
        if (ally.Incarnation != incarnation || ally.Area.Id != area)
            throw new RulesNotImplementedException("the ally selected for the paid effect moved before entry");
        entryPayment ??= EntryPayment.Capture(ally, cast.Player, payment);
    }

    private bool ScheduleEntry(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        // rr:leaves-play.1: a returning card is "a new copy of the card."
        if (ally!.Incarnation != incarnation)
            throw new RulesNotImplementedException("the ally selected for the paid effect changed incarnation");
        if (!AllyCardEntry.PutPaid(cast.World, cast.World.Facts, execution.cardPlayAbilities,
                entryPayment!, cast.Trigger, cast.Events)) return Finish(cast);
        entryScheduled = true;
        cast.Suspend();
        return false;
    }

    private static bool Finish(AbilityResolutionState cast)
    {
        cast.ResolveEffect();
        return true;
    }
}
