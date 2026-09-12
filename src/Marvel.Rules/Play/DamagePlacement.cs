using static Marvel.Rules.Play.Damage;
using static Marvel.Rules.Play.DamagePlacement;
using static Marvel.Rules.Play.DamageAttacks;
using static Marvel.Rules.Play.DamageRecovery;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Places damage and settles resulting defeat.</summary>
public static class DamagePlacement
{

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
        => DealOutcome(world, facts, source, target, amount, trigger, verb, events, by)
            == Damage.Outcome.Defeated;

    /// <summary>Deals damage while preserving a possible procedure suspension.</summary>
    public static Damage.Outcome DealOutcome(
        World world, ICardFacts facts, Card source, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, int by = -1)
        => DealWithAmounts(
            world, facts, source, target, amount, trigger, verb, events,
            out _, out _, by);

    /// <summary>Deals damage and reports the distinct dealt and taken amounts.</summary>
    internal static Damage.Outcome DealWithAmounts(
        World world, ICardFacts facts, Card source, Card target, long amount,
        string trigger, string verb, List<GameEvent> events,
        out long dealt, out long taken, int by = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        var placed = Place(
            world, facts, source, target, amount, trigger, verb, events);
        dealt = placed.Dealt;
        taken = placed.Taken;
        return FinishPlaced(
            world, facts, source, placed, trigger, verb, events, by);
    }

    /// <summary>
    /// Resolve damage through placement while deferring defeat, so simultaneous
    /// damage can be placed on every recipient before any one leaves play.
    /// </summary>
    internal static PlacedDamage Place(
        World world, ICardFacts facts, Card source, Card target, long amount,
        string trigger, string verb, List<GameEvent> events)
    {
        var prepared = Prepare(
            world, facts, source, target, amount, trigger, events);
        ApplyPlaced(world, facts, prepared, trigger, verb, events);
        return prepared;
    }

    /// <summary>Resolve damage through step 4 without changing hit points.</summary>
    internal static PlacedDamage Prepare(
        World world, ICardFacts facts, Card source, Card target, long amount,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        amount = Replace(world, target, source, amount, events);
        return PrepareAfterReplacement(
            world, facts, source, target, amount, trigger, events);
    }

    /// <summary>Resolve damage step 1 and return the still-imminent amount.</summary>
    internal static long Replace(
        World world, Card target, Card source, long amount, List<GameEvent> events)
    {
        if (amount <= 0)
        {
            return 0;
        }

        // `rr:cannot` -- "cannot" is absolute. A character forbidden from
        // taking damage from this source never reaches the damage sequence:
        // there is no imminent damage to replace and no tough card to spend.
        if (!world.DamageAbilities.CanTakeDamage(world, target, source))
        {
            return 0;
        }

        // `rr:damage.step.1` -- "abilities that trigger when [character] would
        // be dealt any amount of damage", which is where a replacement effect
        // sits. It comes before the tough card, which is step 2, and a card
        // that replaces all of the damage leaves nothing for the rest of the
        // nine steps to do.
        return world.DamageAbilities.WouldBeDealt(world, target, source, amount, events);
    }

    /// <summary>Continue damage after step 1 has fixed the amount dealt.</summary>
    internal static PlacedDamage PrepareAfterReplacement(
        World world, ICardFacts facts, Card source, Card target, long amount,
        string trigger, List<GameEvent> events)
    {
        var assignment = DamageAssignment.AfterReplacement(
            amount, amount > 0 && Statuses.Has(world, target, Statuses.Tough));
        if (assignment.Dealt <= 0)
        {
            return new PlacedDamage(target, 0, 0);
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
        // zero-dealt assignment returns before any status is discarded.
        if (assignment.SpendsTough)
        {
            world.DamageAbilities.DamagePreventedByTough(world, target, source, events);
            var tough = world.Areas
                .Where(area => area.Type == DeckType.StatusArea && area.Host == target.ObjectId)
                .SelectMany(area => area.Cards)
                .First(status => status.FaceId == Statuses.Tough);

            Discard.Card(world, tough, trigger, events);

            // `rr:tough.3`: "as a tough status card prevents damage fully, the
            // character who had the tough status card is **not considered to
            // have taken damage**." So no health event, and no defeat.
            return new PlacedDamage(target, assignment.Dealt, assignment.Taken);
        }

        // `rr:damage.step.3` and `.3.2`: modifying what the character takes
        // (including prevention) does not rewrite the amount the source dealt.
        // Step 1 has already fixed that dealt amount; only placement below uses
        // the reduced taken amount.
        assignment = assignment.AfterPrevention(
            world.DamageAbilities.WouldTake(world, target, source, assignment.Taken, events));
        return new PlacedDamage(target, assignment.Dealt, assignment.Taken);
    }

    /// <summary>Place one already-fixed share of simultaneous damage at step 5.</summary>
    internal static void ApplyPlaced(
        World world, ICardFacts facts, PlacedDamage placed,
        string trigger, string verb, List<GameEvent> events)
    {
        if (!placed.Landed)
        {
            return;
        }

        long printed = Health(world, facts, placed.Target);
        long before = Math.Max(0, printed - placed.Target.Damage);
        placed.Target.TakeDamage(placed.Taken);
        long after = Math.Max(0, printed - placed.Target.Damage);
        events.Add(new FieldSet(placed.Target.ObjectId, "health", before, after)
        {
            Trigger = trigger,
            Verb = verb,
            Subjects = FacedownDrones.Is(placed.Target)
                ? new Dictionary<int, string>
                { [placed.Target.ObjectId] = FacedownDrones.EffectiveTitle }
                : null,
        });
    }

    /// <summary>Resolve damage step 6 and defeat after step 5 placement.</summary>
    internal static Damage.Outcome FinishPlaced(
        World world, ICardFacts facts, Card source, PlacedDamage placed,
        string trigger, string verb, List<GameEvent> events, int by = -1,
        Occurrence? recordDefeatOn = null)
    {
        // `rr:leaves-play.1`: after a card leaves play, it is "considered to
        // be a new copy of the card." A delayed effect can make that happen
        // between damage step 5 and defeat; the old copy cannot then proceed
        // through damage steps 6 through 8.
        if (!placed.Landed || !DeckTypes.IsInPlay(placed.Target.Area.Type))
        {
            return Damage.Outcome.NotDefeated;
        }

        var target = placed.Target;
        long after = Math.Max(0, Health(world, facts, target) - target.Damage);

        // `rr:damage.step.6` -- abilities that trigger "when [character]
        // would be defeated". Step 5 has placed the damage, so the condition
        // is now knowable without predicting through damage replacement or a
        // tough status card. An ability may change that imminent defeat, and
        // `rr:would.1` then makes the original condition invalid.
        if (after <= 0)
        {
            int beforeProcedure = world.Agenda.Count;
            if (!world.DamageAbilities.WouldBeDefeated(
                    world, target, source, trigger, verb, by, events,
                    recordDefeatOn))
            {
                return world.Agenda.Count > beforeProcedure
                    ? Damage.Outcome.Suspended
                    : Damage.Outcome.NotDefeated;
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
        if (after > 0)
        {
            return Damage.Outcome.NotDefeated;
        }

        int beforeDefeat = world.Agenda.Count;
        bool defeated = Defeat.Character(
            world, facts, target, trigger, events, how: verb, by: by,
            recordOn: recordDefeatOn);
        if (!defeated && world.Agenda.Count > beforeDefeat)
        {
            return Damage.Outcome.Suspended;
        }
        return defeated ? Damage.Outcome.Defeated : Damage.Outcome.NotDefeated;
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
}
