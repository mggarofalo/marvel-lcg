using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Applies the dealing, reveal and cleanup stages of encounter resolution.</summary>
internal static class EncounterRevealStages
{
    internal static bool TryApply(World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case SurgeKeywordOperation.Condition:
                var surge = step.Require<SurgeKeywordOperation>();
                bool dealt = Deal.EncounterCard(world, step.Seat, surge.DealTrigger, events) is not null;
                RevealKeywords.CompleteKeyword(surge.Origin, surge.Ability, dealt);
                return true;
            case Steps.DealEncounterCards:
                EncounterRevealProcedure.DealEncounterCards(world, facts, events);
                return true;
            case Steps.RevealEncounterCards:
                EncounterRevealProcedure.RevealNextEncounterCard(world, step);
                return true;
            case Steps.RevealEncounterCard:
                EncounterRevealProcedure.RevealEncounterCard(
                    world, facts, world.RevealAbilities, world.Cards[step.Subject],
                    step.Seat, step.Round, events);
                return true;
            case Steps.DiscardRevealedTreachery:
                EncounterRevealDiscard.Resolve(
                    world, facts, step, events);
                return true;
            default:
                return false;
        }
    }
}
