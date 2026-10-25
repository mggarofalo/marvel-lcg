using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityPaymentInstructions;

namespace Marvel.Cards.Run;

/// <summary>Separates public random-cost affordability from committed seeded selection.</summary>
internal static class AbilityRandomHandCost
{
    internal static bool CanPay(World world, Card source, int player, AbilityCost.RandomDiscardFromHand cost)
    {
        if (EffectiveCards.Kind(source, world.Facts) == CardKind.Event)
            throw new RulesNotImplementedException("random discard arrow costs on events require atomic printed payment support");
        return player >= 0 && AbilityCostSelection.HandCandidates(world, source, player).Count >= cost.Count;
    }

    internal static bool CanPayTogether(World world, Card source, int player, IReadOnlyList<AbilityCost> steps)
    {
        var random = steps.OfType<AbilityCost.RandomDiscardFromHand>().ToArray();
        if (random.Length == 0) return true;
        if (random.Length != 1 || steps.Any(step => step is not (AbilityCost.RandomDiscardFromHand or AbilityCost.Exhaust)))
            throw new RulesNotImplementedException("random hand-discard costs support one random instruction combined with fixed exhaustion only");
        return CanPay(world, source, player, random[0]);
    }

    internal static IReadOnlyList<Card> Pay(World world, RandomDiscardHand payment, int player,
        string trigger, List<GameEvent> events)
    {
        var candidates = payment.Cards.Select(card => card.RequireCurrent()).ToList();
        if (candidates.Count < payment.Count || candidates.Any(card => card.Area != payment.Hand))
            throw new RulesNotImplementedException("the hand changed before its random discard cost committed");
        var discarded = new List<Card>();
        int start = events.Count;
        for (int count = 0; count < payment.Count; count++)
        {
            var card = world.Random.Choice(candidates);
            candidates.Remove(card);
            Discard.Card(world, card, trigger, events);
            discarded.Add(card);
        }
        ActivityBatches.Complete(events, start, "Discard", player,
            included: discarded.Select(card => card.ObjectId).ToArray());
        return discarded;
    }
}
