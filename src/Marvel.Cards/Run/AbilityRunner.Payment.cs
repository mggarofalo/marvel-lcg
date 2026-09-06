using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static void DiscardEvent(Card card, Cast cast)
    {
        bool playedInWindow = !cast.Suspended
            && cast.World.Facts.Kind(card.FaceId) == CardKind.Event
            && card.Area.Type == DeckType.RevealingArea
            && card.Area.PlayArea == PlayArea.Of(card.Owner)
            && !cast.Occurrence.Is(Steps.TurnAction);
        if (!cast.Suspended
            && cast.World.Facts.Kind(card.FaceId) == CardKind.Event
            && card.Area.Type == DeckType.RevealingArea
            && card.Area.PlayArea == PlayArea.Of(card.Owner))
        {
            Rules.Play.Discard.Card(cast.World, card, CardPlay.Verb, cast.Events);
            foreach (var payment in cast.World.Effects.Active().Where(effect =>
                effect.Card == card.ObjectId
                && effect.Kind.StartsWith("paid:", StringComparison.Ordinal)).ToList())
            {
                cast.World.Effects.Use(payment);
            }

            if (playedInWindow)
            {
                cast.World.Agenda.NowEventPlayed(
                    cast.World.Agenda.Current?.Round ?? 0,
                    card.ObjectId,
                    cast.Player);
            }
        }
    }

    private static void ApplyPayment(AbilityPaymentResult result, Cast cast)
    {
        if (result.Healed is { } healed) cast.Results["healed"] = healed;
        if (result.Energy is { } energy) cast.Results["energy"] = energy;
        if (result.Suspended)
        {
            // The initiation entry point owns the post-cost continuation.
            // A cost is not a node in the post-arrow effect's structural path.
            cast.Results["costProcedurePending"] = 1;
            cast.Suspend();
        }
    }
}
