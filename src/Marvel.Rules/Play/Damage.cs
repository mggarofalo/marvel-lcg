using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// Dealing damage to a character — <c>rr:damage</c>.
/// </summary>
/// <remarks>
/// <para>
/// One path, because <c>rr:damage</c> is one rule however the damage arrived.
/// An enemy attacking a hero, a hero attacking a minion and a card ability
/// dealing 1 all reduce remaining hit points the same way and all check for
/// defeat the same way.
/// </para>
/// <para>
/// <c>rr:hit-points.2</c> and <c>.3</c> describe two different bookkeepings —
/// a dial for identities and villains, tokens for allies and minions — but
/// they are the same arithmetic and the digest records the result of it,
/// <c>health</c>, for all four. See <c>Card.Damage</c>.
/// </para>
/// </remarks>
public static class Damage
{
    /// <summary>
    /// Deals damage to a character, and defeats it if that was enough.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="target">Who takes it.</param>
    /// <param name="amount">How much. Zero or less does nothing.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="verb">What kind of thing caused it.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>Whether the target was defeated.</returns>
    public static bool Deal(
        World world, ICardFacts facts, Card target, long amount,
        string trigger, string verb, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        if (amount <= 0)
        {
            return false;
        }

        // `rr:tough.2`: "if a character with a tough status card would take any
        // amount of damage, **prevent all of that damage** and discard a tough
        // status card from that character instead." All of it, however much --
        // and `.2.1`, only one card per instance of damage.
        //
        // `rr:tough.2.2` is why this is checked here rather than earlier: "when
        // a hero with a tough status card defends an attack, they reduce the
        // damage from the attack by their DEF **first**. If the damage is
        // reduced to 0, the hero does not lose their tough status card." The
        // `amount <= 0` return above is that clause.
        if (Statuses.Has(world, target, Statuses.Tough))
        {
            var tough = world.Areas
                .Where(area => area.Type == DeckType.StatusArea && area.Host == target.ObjectId)
                .SelectMany(area => area.Cards)
                .First(status => status.FaceId == Statuses.Tough);

            Discard.Card(world, tough, trigger, events);

            // `rr:tough.3`: "as a tough status card prevents damage fully, the
            // character who had the tough status card is **not considered to
            // have taken damage**." So no health event, and no defeat.
            return false;
        }

        long printed = Health(world, facts, target);
        long before = Math.Max(0, printed - target.Damage);
        target.TakeDamage(amount);
        long after = Math.Max(0, printed - target.Damage);

        events.Add(new FieldSet(target.ObjectId, "health", before, after)
        {
            Trigger = trigger, Verb = verb,
        });

        // `rr:defeat`: "if a character has zero or fewer remaining hit points
        // [...] it is defeated". Not "less than zero" -- exactly zero is a
        // defeat, which is why this compares remaining against zero rather
        // than damage against printed.
        return after <= 0 && Defeat.Character(world, facts, target, trigger, events);
    }

    /// <summary>
    /// A character's maximum hit points as the game currently counts them.
    /// </summary>
    /// <remarks>
    /// <c>rr:hit-points.2.3</c>: an ability that says a character "gets +X hit
    /// points" moves the dial, so this is the modified value and not the
    /// printed one.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="character">Whose hit points.</param>
    public static long Health(World world, ICardFacts facts, Card character)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(character);
        // The printed value plus whatever is modifying it. `health` is not in
        // `StateFields`' printed-attribute map -- remaining hit points are
        // computed, not printed -- so `Modified` on it returns the modifiers
        // alone, which is exactly the second half of this sum.
        return facts.PrintedValue(character.FaceId, "HP", world.Players)
            + StateFields.Modified(world, character, "health", facts, world.Players);
    }

    /// <summary>
    /// A character that was attacked hits back — <c>rr:retaliate-x</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "<b>Forced Response</b>: after this character is attacked, deal X damage
    /// to the attacker." So it happens after the attack has resolved, not as
    /// part of the damage.
    /// </para>
    /// <para>
    /// <c>rr:retaliate-x.2</c>: "the character with retaliate X <b>must be in
    /// play after the attack resolves</b> to deal this damage" — an attack that
    /// defeated it kills the retaliation with it.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="attacked">Who was attacked.</param>
    /// <param name="attacker">Who attacked them.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Retaliate(
        World world, ICardFacts facts, Card attacked, Card attacker,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(attacked);
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(events);

        if (!DeckTypes.IsInPlay(attacked.Area.Type))
        {
            return;
        }

        long retaliate = StateFields.Modified(
            world, attacked, "retaliate", facts, world.Players);

        Deal(world, facts, attacker, retaliate, trigger, "Retaliate", events);
    }

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
    public static void Heal(
        World world, ICardFacts facts, Card target, long amount,
        string trigger, string verb, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        if (amount <= 0 || target.Damage == 0)
        {
            return;
        }

        long printed = Health(world, facts, target);
        long before = Math.Max(0, printed - target.Damage);
        target.TakeDamage(-amount);
        long after = Math.Max(0, printed - target.Damage);

        events.Add(new FieldSet(target.ObjectId, "health", before, after)
        {
            Trigger = trigger, Verb = verb,
        });
    }
}
