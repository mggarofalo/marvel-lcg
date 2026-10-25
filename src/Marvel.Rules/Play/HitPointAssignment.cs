using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Assigns a positive remaining value to a character's hit-point dial.</summary>
public static class HitPointAssignment
{
    /// <summary>Sets the dial independently of excess lethal damage already recorded.</summary>
    public static bool Set(World world, Card target, long remaining, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);
        long maximum = DamagePlacement.Health(world, world.Facts, target);
        long damage = RequiredDamage(world, target, remaining, maximum);
        // rr:hit-points.2: "An identity’s or villain’s hit point dial represents their remaining hit points."
        // Engine choice: retain the existing damage representation and health FieldSet event.
        long before = Math.Max(0, maximum - target.Damage);
        target.TakeDamage(damage - target.Damage);
        if (before == remaining) return false;
        events.Add(new FieldSet(target.ObjectId, "health", before, remaining) { Trigger = trigger, Verb = "Set" });
        return true;
    }

    /// <summary>Validates the supported dial assignment against a live or projected maximum.</summary>
    public static long RequiredDamage(World world, Card target, long remaining, long maximum)
    {
        var kind = EffectiveCards.Kind(target, world.Facts);
        if (!DeckTypes.IsInPlay(target.Area.Type)
            || kind is not (CardKind.Hero or CardKind.AlterEgo) && !CardKinds.IsVillain(kind))
            throw new RulesNotImplementedException("hit-point assignment requires an in-play identity or villain dial");
        if (remaining <= 0 || remaining > maximum)
            throw new RulesNotImplementedException("hit-point assignment requires a positive value within the modified maximum");
        return maximum - remaining;
    }
}
