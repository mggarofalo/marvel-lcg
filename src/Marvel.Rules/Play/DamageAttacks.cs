using static Marvel.Rules.Play.Damage;
using static Marvel.Rules.Play.DamagePlacement;
using static Marvel.Rules.Play.DamageAttacks;
using static Marvel.Rules.Play.DamageRecovery;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Applies attack damage, overkill, consequential damage, and retaliation.</summary>
public static class DamageAttacks
{

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
    public static Damage.AttackResult Attack(
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
    public static Damage.AttackResult Attack(
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
        DiscardPiercedTough(world, facts, attacker, target, amount, trigger, events);

        // Remaining hit points are captured before the damage because the
        // defeated character may leave play before overkill is resolved.
        long remaining = Math.Max(
            0, DamagePlacement.Health(world, facts, target) - target.Damage);
        // The same is true of control. A defeated ally moves to its owner's
        // discard pile, but `rr:overkill.1` sends excess damage to the identity
        // of the player who **controlled** it while it was defeated.
        CardKind attackedKind = FacedownDrones.Kind(target, facts);
        int spillPlayer = attackedKind == CardKind.Ally
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
        bool hasOverkill = Keywords.Has(world, attacker, Keywords.Overkill, facts);
        bool canRetaliate = retaliate
            && !Keywords.Has(world, attacker, Keywords.Ranged, facts);
        var placed = Place(
            world, facts, source, target, amount, trigger, verb, events);
        long dealtAmount = placed.Dealt;
        long takenAmount = placed.Taken;

        // `rr:delayed-effect.1`: the effect resolves immediately when damage
        // has landed. That is after `rr:damage.step.5` places the damage and
        // before steps 6 through 8 can defeat and discard the character. A
        // status created here therefore leaves with a lethally damaged host.
        ResolveLandedDamage(world, target, placed, events);

        var outcome = FinishPlaced(
            world, facts, source, placed, trigger, verb, events,
            by: attacker.Owner);
        // Prevention changes what is taken without changing what was dealt,
        // but `rr:overkill.4` specifically withholds prevented excess. The
        // spill is therefore the taken amount beyond the former hit points.
        long beyond = Math.Max(0, takenAmount - remaining);
        bool overkill = beyond > 0 && hasOverkill;
        ResolveOverkill(
            world, facts, source, attackedKind, spillPlayer, beyond, trigger, events,
            outcome, overkill);

        long dealt = MeasuredDamage(events, firstDamageEvent);

        // Measured rather than assumed. A tough status card prevents all of the
        // damage, so the number on the dial is the only honest answer to
        // whether the character took any.
        RecordDamagedTarget(damaged, target, before);

        // `rr:ranged.1` -- "this attack ignores the retaliate keyword".
        FinishOrSuspendAttack(
            world, facts, attacker, source, target, trigger, events, attackedKind,
            spillPlayer, beyond, overkill, canRetaliate, outcome);

        // `rr:overkill.3`: a card ability that counts excess damage uses the
        // same value the overkill keyword calculated. Expose that calculation
        // rather than asking every card to reconstruct the defeated target's
        // former remaining hit points after it has left play.
        long excess = outcome == Damage.Outcome.Defeated ? beyond : 0;
        return new Damage.AttackResult(
            damaged, dealt, outcome == Damage.Outcome.Suspended, excess,
            dealtAmount, takenAmount);
    }

    private static void DiscardPiercedTough(
        World world, ICardFacts facts, Card attacker, Card target, long amount,
        string trigger, List<GameEvent> events)
    {
        if (amount <= 0 || !Keywords.Has(world, attacker, Keywords.Piercing, facts))
        {
            return;
        }
        foreach (var tough in Statuses.On(world, target, Statuses.Tough).ToList())
        {
            Discard.Card(world, tough, trigger, events);
        }
    }

    private static void ResolveLandedDamage(
        World world, Card target, Damage.PlacedDamage placed, List<GameEvent> events)
    {
        if (placed.Landed)
        {
            DelayedEffects.Occur(world, Steps.DamageDealt, target.ObjectId, events);
        }
    }

    private static void ResolveOverkill(
        World world, ICardFacts facts, Card source, CardKind attackedKind,
        int spillPlayer, long beyond, string trigger, List<GameEvent> events,
        Damage.Outcome outcome, bool overkill)
    {
        if (outcome == Damage.Outcome.Defeated && overkill)
        {
            Spill(world, facts, source, attackedKind, spillPlayer, beyond, trigger, events);
        }
    }

    private static long MeasuredDamage(List<GameEvent> events, int firstDamageEvent) =>
        events.Skip(firstDamageEvent)
            .OfType<FieldSet>()
            .Where(change => change.Field == "health"
                && change.From is { } from && change.To is { } to && from > to)
            .Sum(change => change.From!.Value - change.To!.Value);

    private static void RecordDamagedTarget(List<Card> damaged, Card target, long before)
    {
        if (target.Damage != before)
        {
            damaged.Add(target);
        }
    }

    private static void FinishOrSuspendAttack(
        World world, ICardFacts facts, Card attacker, Card source, Card target,
        string trigger, List<GameEvent> events, CardKind attackedKind, int spillPlayer,
        long beyond, bool overkill, bool canRetaliate, Damage.Outcome outcome)
    {
        if (outcome == Damage.Outcome.Suspended)
        {
            world.Agenda.Then(new PhaseStep(
                Steps.FinishAttackDamage,
                world.Agenda.Current?.Round ?? 0,
                5,
                Subject: target.ObjectId,
                Seat: spillPlayer,
                Plan: true,
                Character: attacker.ObjectId,
                ProcedureSource: source.ObjectId,
                ProcedureTrigger: trigger,
                ProcedureVerb: attackedKind.ToString(),
                ProcedureAmount: beyond,
                ProcedureFlag: overkill,
                FinalStep: canRetaliate));
        }
        else if (canRetaliate)
        {
            Retaliate(world, facts, target, attacker, trigger, events);
        }
    }

    /// <summary>Resolve overkill and retaliate after a suspended defeat decision.</summary>
    public static void FinishAttack(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var target = world.Cards[step.Subject];
        var attacker = world.Cards[step.Character];
        var source = world.Cards[step.ProcedureSource];
        bool defeated = !DeckTypes.IsInPlay(target.Area.Type);
        if (defeated && step.ProcedureFlag && step.ProcedureAmount > 0)
        {
            Spill(
                world, facts, source,
                Enum.Parse<CardKind>(step.ProcedureVerb, ignoreCase: false),
                step.Seat, step.ProcedureAmount, step.ProcedureTrigger, events);
        }
        if (step.FinalStep)
        {
            Retaliate(world, facts, target, attacker, step.ProcedureTrigger, events);
        }
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
    internal static void Spill(
        World world, ICardFacts facts, Card source, CardKind defeatedKind,
        int controllingPlayer, long beyond,
        string trigger, List<GameEvent> events)
    {
        var onto = defeatedKind switch
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
            DamagePlacement.Deal(
                world, facts, source, onto, beyond, trigger, Keywords.Overkill, events);
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

        // `rr:villain-defeat.3.1`: same-title villain stages are the same
        // character for card abilities. The attacked physical stage may have
        // left play during damage, while the character that was attacked is
        // now represented by the next stage.
        if (!DeckTypes.IsInPlay(attacked.Area.Type)
            && CardKinds.IsVillain(facts.Kind(attacked.FaceId))
            && world.TheCardIn(DeckType.VillainArea) is { } next
            && string.Equals(
                facts.Title(attacked.FaceId), facts.Title(next.FaceId),
                StringComparison.Ordinal))
        {
            attacked = next;
        }

        if (!DeckTypes.IsInPlay(attacked.Area.Type))
        {
            return;
        }

        long retaliate = StateFields.Modified(
            world, attacked, "retaliate", facts, world.Players);

        DamagePlacement.Deal(
            world, facts, attacked, attacker, retaliate, trigger, "Retaliate", events);
    }
}
