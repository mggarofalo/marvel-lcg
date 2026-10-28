using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Completes an accepted payment choice without replaying the player's answer.</summary>
internal sealed class AbilityChoicePayment(ResourcePaymentReceipt payment, Card? ally = null)
    : AbilityPaymentPlan
{
    private readonly int incarnation = ally?.Incarnation ?? -1;
    private bool windowOpened;

    internal override bool Commit(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        if (!payment.IsComplete)
            throw new InvalidOperationException("payment choice resumed before its resource costs completed");
        if (!windowOpened)
        {
            windowOpened = true;
            if (ResourceSpentWindow.Open(cast.World, payment.SpentCards, cast.Player))
            {
                cast.Suspend();
                return false;
            }
        }
        if (ally is not null)
        {
            // rr:leaves-play.1: a returning card is "a new copy of the card."
            if (ally.Incarnation != incarnation)
                throw new RulesNotImplementedException("the ally selected for the paid effect changed incarnation");
            CardPlay.PutAllyIntoPlay(cast.World, cast.World.Facts, execution.cardPlayAbilities,
                ally, cast.Player, cast.Trigger, cast.Events);
        }
        cast.ResolveEffect();
        return true;
    }
}
