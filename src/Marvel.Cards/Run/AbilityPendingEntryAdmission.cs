using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Out-of-play admission is limited to the pending ally's own entry interrupt.
internal static class AbilityPendingEntryAdmission
{
    internal static bool OwnEntry(Card card, Occurrence occurrence) =>
        occurrence.Entry.PendingAllyEntry?.MatchesPending(card) == true;

    internal static bool Allows(Card card, CompiledCardAbility ability, Occurrence occurrence) =>
        !OwnEntry(card, occurrence) || AbilityTypes.IsInterrupt(ability.Trigger.Timing)
            && ability.Trigger.Event == Steps.CardEntersPlay
            && ability.Trigger.Subject == AbilitySubjects.This;
}
