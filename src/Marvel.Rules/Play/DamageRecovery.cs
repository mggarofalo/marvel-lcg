using static Marvel.Rules.Play.Damage;
using static Marvel.Rules.Play.DamagePlacement;
using static Marvel.Rules.Play.DamageAttacks;
using static Marvel.Rules.Play.DamageRecovery;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Heals and moves existing damage.</summary>
public static class DamageRecovery
{

    /// <summary>
    /// Heals damage from a character — <c>rr:heal</c>.
    /// </summary>
    /// <remarks>
    /// <c>rr:heal.1</c>: "a heal effect can only bring a character to its
    /// maximum hit points", which <c>Card.TakeDamage</c>'s clamp at zero
    /// already is — there is no way to have negative damage.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="target">Who is healed.</param>
    /// <param name="amount">How much.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="verb">What kind of thing caused it.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>
    /// How much damage was <b>actually</b> healed, which is not the amount
    /// asked for.
    /// </returns>
    /// <remarks>
    /// The return value is not bookkeeping. Cards are written against it —
    /// "Rhino heals 4 damage. <b>If no damage was healed this way</b>, this
    /// card gains surge" — and a character at full health, or damaged by less
    /// than the amount, heals less than it was told to. Asking first is
    /// silently wrong for the same reason: a pre-check reads a number that the
    /// heal may not reach.
    /// </remarks>
    public static long Heal(
        World world, ICardFacts facts, Card target, long amount,
        string trigger, string verb, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        if (amount <= 0 || target.Damage == 0)
        {
            return 0;
        }

        long printed = DamagePlacement.Health(world, facts, target);
        long before = Math.Max(0, printed - target.Damage);

        // Bounded by the damage there is: healing 4 from a character with 1
        // damage heals 1. `Card.Damage` cannot go below zero and neither can
        // the answer.
        long healed = Math.Min(amount, target.Damage);
        target.TakeDamage(-healed);
        long after = Math.Max(0, printed - target.Damage);

        events.Add(new FieldSet(target.ObjectId, "health", before, after)
        {
            Trigger = trigger,
            Verb = verb,
            Subjects = FacedownDrones.Is(target)
                ? new Dictionary<int, string>
                { [target.ObjectId] = FacedownDrones.EffectiveTitle }
                : null,
        });

        return healed;
    }

    /// <summary>Moves damage from one character to another — <c>rr:move</c>.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:move.2</c> requires both a valid source and destination. A
    /// destination forbidden from taking this source's damage invalidates the
    /// move, so no damage is healed from the origin as a side effect.
    /// </para>
    /// <para>
    /// <c>rr:move.3.1</c> moves the same amount off the origin and onto the
    /// destination, bounded by the damage actually present. <c>rr:move.4</c>
    /// considers the first half healing, and <c>rr:move.5</c> considers the
    /// second half dealt damage, so both halves use the ordinary rule paths.
    /// Prevention at the destination changes damage taken, not the amount
    /// dealt (<c>rr:damage.3.2</c>), and therefore does not undo the healing.
    /// </para>
    /// </remarks>
    /// <returns>The amount moved off <paramref name="from"/>.</returns>
    public static long MoveDamage(
        World world, ICardFacts facts, Card source, Card from, Card to, long amount,
        string trigger, string verb, List<GameEvent> events, int by = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(events);

        long moved = Math.Min(Math.Max(0, amount), from.Damage);
        if (moved <= 0
            || ReferenceEquals(from, to)
            || !world.DamageAbilities.CanTakeDamage(world, to, source))
        {
            return 0;
        }

        long healed = Heal(world, facts, from, moved, trigger, verb, events);
        DamagePlacement.Deal(
            world, facts, source, to, healed, trigger, verb, events, by);
        return healed;
    }
}
