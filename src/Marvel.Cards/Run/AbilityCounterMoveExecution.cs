using static Marvel.Cards.Run.AbilityCardStateExecution;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;

namespace Marvel.Cards.Run;

/// <summary>Moves named counters between two cards.</summary>
internal static class AbilityCounterMoveExecution
{
    internal static void Run(AbilityCounterEffect.MoveCounters move, AbilityCardStateContext context)
    {
        // The move does as much as it can: a card gives only the counters it has.
        if (Find(move.From, context) is not { } from || Find(move.To, context) is not { } to
            || from.ObjectId == to.ObjectId) return;
        string key = "c_" + move.Counter;
        long count = Math.Min(move.Count, from.Tokens.GetValueOrDefault(key));
        if (count <= 0) return;
        AbilityCardOperations.RemoveCounters(context.World, context.CardPlayAbilities, from, move.Counter,
            count, context.Trigger, context.Events);
        long before = to.Tokens.GetValueOrDefault(key);
        to.PlaceTokens(key, count);
        // Engine choice: the arriving counters are reported with their own verb.
        context.Events.Add(new FieldSet(to.ObjectId, key, before, before + count)
        { Trigger = context.Trigger, Verb = "Move_Counters" });
    }
}
