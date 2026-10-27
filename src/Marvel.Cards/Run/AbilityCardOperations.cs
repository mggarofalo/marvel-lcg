using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Card mutations shared by cost commitment and effect execution.</summary>
internal static class AbilityCardOperations
{
    internal static bool Exhaust(Card target, string trigger, List<GameEvent> events)
    {
        if (!target.Ready) return false;
        target.Exhaust();
        events.Add(new FieldSet(target.ObjectId, "is_exhaust", 0, 1)
        {
            Trigger = trigger,
            Verb = "Exhaust",
        });
        return true;
    }

    internal static void RemoveCounters(
        World world, ICardCounterPools pools, Card card, string type, long count,
        string trigger, List<GameEvent> events)
    {
        string key = AbilityCostSelection.CounterKeyForRemoval(card, type, count)
            ?? throw new RulesNotImplementedException(
                $"'{card.FaceId}' has fewer than {count} {type} counters");
        RemovePhysicalCounters(world, card, type, count, key, trigger, events);

        if (CounterRemovalDeparts(world, pools.CounterPool(world, card), card, card.InstanceState))
        {
            if (!VictoryDisplay.ToVictoryDisplay(world, world.Facts, card, trigger, events))
                Discard.Card(world, card, trigger, events);
        }
    }

    internal static bool CounterRemovalDeparts(
        World world, CardCounterPool? pool, Card card, CardInstanceState quantities) =>
        AbilityExpressionEvaluation.CounterCount(quantities, "allPurpose") == 0
        && !Characteristics.IsLost(world, card, "uses")
        && pool?.Uses == true;

    private static void RemovePhysicalCounters(World world, Card card, string type,
        long count, string key, string trigger, List<GameEvent> events)
    {
        if (GreenGoblinTransition.HandlesCounterRemoval(card, type))
        {
            GreenGoblinTransition.RemoveCounters(world, card, type, count, trigger, events);
            return;
        }
        long before = card.Tokens.GetValueOrDefault(key);
        card.PlaceTokens(key, -count);
        events.Add(new FieldSet(card.ObjectId, key, before, before - count)
        {
            Trigger = trigger,
            Verb = "Remove_Counter",
        });
    }
}
