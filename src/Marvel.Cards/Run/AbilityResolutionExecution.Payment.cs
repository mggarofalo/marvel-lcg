using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionPayment
{
    internal static void DiscardEvent(this AbilityResolutionExecution execution, Card card, AbilityResolutionState cast)
    {
        if (IsResolvingEvent(cast, card))
        {
            bool playedInWindow = !cast.Occurrence.Is(Steps.TurnAction);
            Rules.Play.Discard.Card(cast.World, card, CardPlay.Verb, cast.Events);
            ConsumePaymentMarkers(cast.World, card);

            if (playedInWindow)
            {
                cast.World.Agenda.NowEventPlayed(
                    cast.World.Agenda.Current?.Round ?? 0,
                    card.ObjectId,
                    cast.Player);
            }
        }
    }

    private static bool IsResolvingEvent(AbilityResolutionState cast, Card card) =>
        !cast.Suspended
        && cast.World.Facts.Kind(card.FaceId) == CardKind.Event
        && card.Area.Type == DeckType.RevealingArea
        && card.Area.PlayArea == PlayArea.Of(card.Owner);

    private static void ConsumePaymentMarkers(World world, Card card)
    {
        foreach (var payment in world.Effects.Active().Where(effect =>
            effect.Card == card.ObjectId
            && effect.Kind.StartsWith("paid:", StringComparison.Ordinal)).ToList())
            world.Effects.Use(payment);
    }

    internal static void ApplyPayment(this AbilityResolutionExecution execution, AbilityPaymentResult result, AbilityResolutionState cast)
    {
        if (result.Healed is { } healed) cast.Results["healed"] = healed;
        if (result.Energy is { } energy) cast.Results["energy"] = energy;
        if (result.Suspended)
        {
            cast.Suspend();
        }
    }
}
