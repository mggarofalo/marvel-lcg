using static Marvel.Cards.Run.AbilityAdmission;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

internal static class AbilityAllyEntry
{
    internal static bool HasTarget(AbilityEffect.PutIntoPlay effect, AbilityAdmissionScope cast)
    {
        var card = cast.Context.Selectors().Find(effect.Card);
        return card is not null && (!effect.AllyUnderYourControl
            || AllyCardEntry.CanPut(cast.World, card, cast.Player));
    }

    internal static bool TryRun(AbilityResolutionExecution execution, AbilityEffect effect,
        AbilityResolutionState cast)
    {
        if (effect is not AbilityEffect.PutIntoPlay { AllyUnderYourControl: true } entry)
            return false;
        var card = execution.Find(entry.Card, cast);
        if (card is null || !AllyCardEntry.CanPut(cast.World, card, cast.Player))
        {
            cast.CompletedOutcome = AbilityStructuralOutcome.None;
            return true;
        }
        var owner = cast.World.Agenda.Current;
        var occurrence = cast.World.Agenda.Occurrence;
        CardPlay.PutAllyIntoPlay(cast.World, cast.World.Facts, execution.cardPlayAbilities,
            card, cast.Player, cast.Trigger, cast.Events);
        // The instruction follows this physical card into play. Later entry windows
        // may still make that new incarnation stale before the suffix resumes.
        if (entry.Card is AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen }
            or AbilityCardSelection.DefeatedAllyInYourDiscardPile)
            cast.ChooseSelection(card);
        cast.CompletedOutcome = AbilityStructuralOutcome.Full;
        cast.ResolveEffect();
        execution.SuspendAfterProcedure(effect, cast, owner, occurrence);
        return true;
    }
}


