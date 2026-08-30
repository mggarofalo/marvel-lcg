using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

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
    /// <summary>The characters and amount actually damaged by one attack.</summary>
    public sealed record AttackResult(IReadOnlyList<Card> Characters, long Amount);

    /// <summary>
    /// Deals damage to a character, and defeats it if that was enough.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="source">The card dealing it.</param>
    /// <param name="target">Who takes it.</param>
    /// <param name="amount">How much. Zero or less does nothing.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="verb">What kind of thing caused it.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="by">
    /// The seat whose character is dealing it, or <c>-1</c>.
    /// <para>
    /// <b>Filled in only where it is unambiguous</b>, which today is an attack:
    /// <c>rr:ownership-and-control.2</c> puts the attacking character under its
    /// owner's control, so the attacker's seat is who did it, and an enemy's
    /// attacker has no seat. Damage from a card ability is left at <c>-1</c> on
    /// purpose — the player resolving an encounter card is the seat it was
    /// dealt to, and calling that "the player who defeated your ally" would be
    /// a plausible answer to a question nobody asked.
    /// </para>
    /// </param>
    /// <returns>Whether the target was defeated.</returns>
    public static bool Deal(
        World world, ICardFacts facts, Card source, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, int by = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        if (amount <= 0)
        {
            return false;
        }

        // `rr:cannot` -- "cannot" is absolute. A character forbidden from
        // taking damage from this source never reaches the damage sequence:
        // there is no imminent damage to replace and no tough card to spend.
        if (!world.Abilities.CanTakeDamage(world, target, source))
        {
            return false;
        }

        // `rr:damage.step.1` -- "abilities that trigger when [character] would
        // be dealt any amount of damage", which is where a replacement effect
        // sits. It comes before the tough card, which is step 2, and a card
        // that replaces all of the damage leaves nothing for the rest of the
        // nine steps to do.
        amount = world.Abilities.WouldBeDealt(world, target, source, amount, events);
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

        // `rr:damage.step.6` -- abilities that trigger "when [character]
        // would be defeated". Step 5 has placed the damage, so the condition
        // is now knowable without predicting through damage replacement or a
        // tough status card. An ability may change that imminent defeat, and
        // `rr:would.1` then makes the original condition invalid.
        if (after <= 0)
        {
            if (!world.Abilities.WouldBeDefeated(
                    world, target, source, trigger, verb, by, events))
            {
                return false;
            }
            after = Math.Max(0, Health(world, facts, target) - target.Damage);
        }

        // `rr:defeat`: "if a character has zero or fewer remaining hit points
        // [...] it is defeated". Not "less than zero" -- exactly zero is a
        // defeat, which is why this compares remaining against zero rather
        // than damage against printed.
        // The verb travels into the defeat, because `rr:defeat` says nothing
        // about what caused one and cards ask: Gene Pool answers "after an ally
        // is defeated **by anything other than consequential damage**", and
        // consequential damage is one of the verbs this is called with.
        return after <= 0
            && Defeat.Character(world, facts, target, trigger, events, how: verb, by: by);
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
        return FacedownDrones.BaseValue(character, facts, "HP", world.Players)
            + StateFields.Modified(world, character, "health", facts, world.Players);
    }

    /// <summary>
    /// One attack, with the keywords that change how it lands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three keywords sit between an attack and its damage and all three are
    /// about the <i>attack</i> rather than either character, which is why they
    /// are one call rather than three checks scattered across two attack paths.
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <c>rr:piercing.1</c> — "before this attack deals damage to a
    ///     character, discard each tough status card from that character", and
    ///     <c>.2</c>: an attack dealing no damage discards none.
    ///   </description></item>
    ///   <item><description>
    ///     <c>rr:overkill.1</c> — the damage beyond a defeated ally's hit points
    ///     goes to its controller's identity, and beyond a defeated minion's to
    ///     the villain. <c>.2</c>: it is "damage from an attack, but does not
    ///     constitute an attack against that character", so it retaliates
    ///     against nothing.
    ///   </description></item>
    ///   <item><description>
    ///     <c>rr:ranged.1</c> — "this attack ignores the retaliate keyword".
    ///   </description></item>
    /// </list>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="attacker">Who is attacking.</param>
    /// <param name="target">Who is being attacked.</param>
    /// <param name="amount">How much damage.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="verb">What kind of thing caused it.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="retaliate">Whether to resolve retaliation before returning.</param>
    /// <returns>
    /// Every character this attack <b>actually damaged</b>, which is not every
    /// character it was aimed at. <c>rr:tough.3</c>: a character whose tough
    /// status card ate the damage "is not considered to have taken damage", and
    /// cards are written against that — "if a character is damaged by this
    /// attack, that character is stunned".
    /// </returns>
    public static AttackResult Attack(
        World world, ICardFacts facts, Card attacker, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, bool retaliate = true)
        => Attack(
            world, facts, attacker, attacker, target, amount, trigger, verb, events, retaliate);

    /// <summary>
    /// One attack whose acting character and damage source are different cards.
    /// </summary>
    /// <remarks>
    /// A card ability labelled as an attack is performed by the resolving
    /// character, but its damage still comes from the card carrying the
    /// ability. Keeping those roles separate lets retaliation hit the actor
    /// while a prohibition such as “cannot take damage from [trait] upgrades”
    /// inspect the actual source. The ordinary character-attack overload uses
    /// the attacker for both roles.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="attacker">The character performing the attack.</param>
    /// <param name="source">The card the damage comes from.</param>
    /// <param name="target">Who is being attacked.</param>
    /// <param name="amount">How much damage.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="verb">What kind of thing caused it.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="retaliate">Whether to resolve retaliation before returning.</param>
    public static AttackResult Attack(
        World world, ICardFacts facts, Card attacker, Card source, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, bool retaliate = true)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:piercing.2` -- "if an attack with the piercing keyword would deal
        // no damage to the attacked character, it does not discard tough status
        // cards from that character", which is why this is inside the guard.
        if (amount > 0 && Keywords.Has(world, attacker, Keywords.Piercing, facts))
        {
            foreach (var tough in Statuses.On(world, target, Statuses.Tough).ToList())
            {
                Discard.Card(world, tough, trigger, events);
            }
        }

        // Worked out before the damage, because `rr:overkill.1` is "the damage
        // beyond its hit points" and after the defeat the character is gone.
        long beyond = Math.Max(0, amount - Math.Max(0, Health(world, facts, target) - target.Damage));
        // The same is true of control. A defeated ally moves to its owner's
        // discard pile, but `rr:overkill.1` sends excess damage to the identity
        // of the player who **controlled** it while it was defeated.
        int spillPlayer = facts.Kind(target.FaceId) == CardKind.Ally
            ? target.Area.PlayArea.Player
            : -1;
        // `rr:overkill.4`: "if excess damage from an attack with overkill is
        // prevented, that damage is **not** dealt to the identity or villain."
        //
        // **Nothing extra is needed for that.** A tough status card prevents all
        // the damage and `Deal` answers false, so the character was not
        // defeated and nothing spills -- `rr:tough.3` again, that a character
        // whose tough card ate the damage "is not considered to have taken
        // damage". A separate check here would be a second statement of the
        // same rule, and only one of them could be right after an edit.
        var damaged = new List<Card>();
        int firstDamageEvent = events.Count;
        long before = target.Damage;
        if (Deal(world, facts, source, target, amount, trigger, verb, events, by: attacker.Owner)
            && beyond > 0
            && Keywords.Has(world, attacker, Keywords.Overkill, facts))
        {
            Spill(world, facts, source, target, spillPlayer, beyond, trigger, events);
        }

        long dealt = events
            .Skip(firstDamageEvent)
            .OfType<FieldSet>()
            .Where(change => change.Field == "health"
                && change.From is { } from && change.To is { } to && from > to)
            .Sum(change => change.From!.Value - change.To!.Value);

        // Measured rather than assumed. A tough status card prevents all of the
        // damage, so the number on the dial is the only honest answer to
        // whether the character took any.
        if (target.Damage != before)
        {
            damaged.Add(target);
        }

        // `rr:ranged.1` -- "this attack ignores the retaliate keyword".
        if (retaliate && !Keywords.Has(world, attacker, Keywords.Ranged, facts))
        {
            Retaliate(world, facts, target, attacker, trigger, events);
        }

        return new AttackResult(damaged, dealt);
    }

    /// <summary>
    /// Excess damage from an attack with overkill — <c>rr:overkill</c>.
    /// </summary>
    /// <remarks>
    /// "If an ally is defeated [...] deal any damage on that ally beyond its hit
    /// points to <b>the identity of the player who controls the ally</b>. If a
    /// minion is defeated [...] to <b>the villain</b>." Two different
    /// destinations, decided by what was defeated rather than by who attacked.
    /// </remarks>
    private static void Spill(
        World world, ICardFacts facts, Card source, Card defeated, int controllingPlayer, long beyond,
        string trigger, List<GameEvent> events)
    {
        var onto = FacedownDrones.Kind(defeated, facts) switch
        {
            CardKind.Ally when controllingPlayer >= 0 =>
                world.Seats[controllingPlayer].IdentityCard,
            CardKind.Minion => world.TheCardIn(DeckType.VillainArea),
            _ => null,
        };

        if (onto is not null)
        {
            // `rr:overkill.2`: "damage dealt by overkill to an identity or
            // villain is considered damage from an attack, but **does not
            // constitute an attack against that character**" -- so this deals
            // damage and does not retaliate.
            Deal(world, facts, source, onto, beyond, trigger, Keywords.Overkill, events);
        }
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

        Deal(world, facts, attacked, attacker, retaliate, trigger, "Retaliate", events);
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

        long printed = Health(world, facts, target);
        long before = Math.Max(0, printed - target.Damage);

        // Bounded by the damage there is: healing 4 from a character with 1
        // damage heals 1. `Card.Damage` cannot go below zero and neither can
        // the answer.
        long healed = Math.Min(amount, target.Damage);
        target.TakeDamage(-healed);
        long after = Math.Max(0, printed - target.Damage);

        events.Add(new FieldSet(target.ObjectId, "health", before, after)
        {
            Trigger = trigger, Verb = verb,
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
            || !world.Abilities.CanTakeDamage(world, to, source))
        {
            return 0;
        }

        long healed = Heal(world, facts, from, moved, trigger, verb, events);
        Deal(world, facts, source, to, healed, trigger, verb, events, by);
        return healed;
    }
}
