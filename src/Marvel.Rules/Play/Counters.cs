using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Moving physical all-purpose counters between cards.</summary>
public static class Counters
{
    /// <summary>Moves counters and applies the destination card's defined type.</summary>
    public static long Move(
        World world, ICardFacts facts, Card from, Card to,
        string fromType, long amount, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(events);
        if (ReferenceEquals(from, to) || amount <= 0)
        {
            return 0;
        }

        string sourceKey = "c_" + fromType;
        long held = from.Tokens.GetValueOrDefault(sourceKey);
        long moved = Math.Min(held, amount);
        if (moved <= 0)
        {
            return 0;
        }

        string destinationType = world.Abilities.CounterPool(world, to)?.Type
            ?? string.Empty;
        string destinationKey = "c_" + (destinationType.Length > 0
            ? destinationType
            : "allPurpose");
        long beforeDestination = to.Tokens.GetValueOrDefault(destinationKey);
        from.PlaceTokens(sourceKey, -moved);
        to.PlaceTokens(destinationKey, moved);
        events.Add(new FieldSet(from.ObjectId, sourceKey, held, held - moved)
        {
            Trigger = trigger, Verb = "Move_Counter",
        });
        events.Add(new FieldSet(
            to.ObjectId, destinationKey, beforeDestination, beforeDestination + moved)
        {
            Trigger = trigger, Verb = "Move_Counter",
        });
        return moved;
    }
}
