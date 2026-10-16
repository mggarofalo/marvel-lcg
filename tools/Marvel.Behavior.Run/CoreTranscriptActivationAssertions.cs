using System.Text.RegularExpressions;
using Marvel.Rules.State;

namespace Marvel.Behavior.Run;

internal static class CoreTranscriptActivationAssertions
{
    internal static void CardStillRevealing(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        if (card.Area.Type != DeckType.RevealingArea)
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} still being revealed; was {card.Area}");
    }

    internal static void CurrentAttackingCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card card = context.SceneRequired(step).Find(SceneCard(match, step));
        int seat = Seat(match, step);
        if (context.World.Attack is not { } attack
            || attack.Enemy != card.ObjectId || attack.Player != seat)
            throw new TranscriptAssertionException(
                $"{step.Location}: expected card {card.ObjectId} attacking seat {seat + 1}; was {context.World.Attack}");
    }
}
