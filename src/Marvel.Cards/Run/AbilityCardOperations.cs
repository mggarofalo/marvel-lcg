using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Card mutations shared by cost commitment and effect execution.</summary>
internal static class AbilityCardOperations
{
    internal static void Exhaust(Card target, string trigger, List<GameEvent> events)
    {
        if (!target.Ready) return;
        target.Exhaust();
        events.Add(new FieldSet(target.ObjectId, "is_exhaust", 0, 1)
        {
            Trigger = trigger, Verb = "Exhaust",
        });
    }

    internal static void RemoveCounters(
        World world, ICardCounterPools pools, Card card, string type, long count,
        string trigger, List<GameEvent> events)
    {
        string key = AbilityCostSelection.CounterKeyForRemoval(card, type, count)
            ?? throw new RulesNotImplementedException(
                $"'{card.FaceId}' has fewer than {count} {type} counters");
        long before = card.Tokens.GetValueOrDefault(key);
        card.PlaceTokens(key, -count);
        events.Add(new FieldSet(card.ObjectId, key, before, before - count)
        {
            Trigger = trigger, Verb = "Remove_Counter",
        });

        if (AbilityExpressionEvaluation.CounterCount(card, "allPurpose") == 0
            && !Characteristics.IsLost(world, card, "uses")
            && pools.CounterPool(world, card)?.Uses == true)
        {
            if (!Defeat.ToVictoryDisplay(world, world.Facts, card, trigger, events))
                Discard.Card(world, card, trigger, events);
        }
    }
}
