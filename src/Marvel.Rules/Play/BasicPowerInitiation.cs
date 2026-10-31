using static Marvel.Rules.Play.BasicPowers;
using static Marvel.Rules.Play.BasicPowerInitiation;
using static Marvel.Rules.Play.BasicPowerResolution;
using static Marvel.Rules.Play.BasicThwartPowers;
using static Marvel.Rules.Play.AllyBasicPowers;
using static Marvel.Rules.Play.BasicPowerStatus;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Starts basic and card-provided attack and thwart powers.</summary>
public static class BasicPowerInitiation
{

    /// <summary>
    /// A basic attack — <c>rr:attack-player-ability-type.1</c>.
    /// </summary>
    /// <remarks>
    /// "A hero or ally can use their basic attack power to attack an enemy. A
    /// character <b>must exhaust</b> to use this power. This deals damage equal
    /// to the character's ATK value to the enemy."
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="player">Who is attacking.</param>
    /// <param name="enemy">Who is being attacked.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void BasicAttack(
        World world, ICardFacts facts, int player, Card enemy, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(events);

        var seat = world.Seats[player];
        var character = seat.IdentityCard;
        Require(world, facts, seat, Forms.Hero, AttackVerb, "ATK");

        // `rr:attack-player-ability-type.1.1`: "a character can only initiate a
        // basic attack if there is an enemy that can be attacked by that
        // character **or if that character is stunned**." A stunned character
        // attacks nothing, on purpose -- it is how the stun comes off.
        // rr:stun-stunned.5.1: "A stunned character can attempt to attack or use
        // an attack ability even if it has no valid target for an attack", and
        // .7: "that character is not considered to have attacked". The enemy a
        // stunned attempt names is therefore never attacked, so a prohibition on
        // attacking it (Fear of Kang) has nothing to forbid. The turn's offer
        // still lists only legal targets when there are any.
        if (!Statuses.Afflicted(world, facts, character, Statuses.Stunned)
            && !Attackable(world, facts, player, character).Any(t => t.ObjectId == enemy.ObjectId))
        {
            // `rr:guard.1` is the case this catches in an ordinary game: a
            // minion with guard engaged with this player makes every villain an
            // illegal target while it is in play.
            throw new RulesNotImplementedException(
                $"card {enemy.ObjectId} is not an enemy {seat.Name} can attack");
        }

        if (BasicAttackCosts.Schedule(world, character, enemy, exhaust: true)) return;
        Exhaust(character, AttackVerb, events);

        // `rr:stun-stunned.1` and `.5`: the attack is cancelled and the stun
        // goes, but the cost was already paid.
        if (BasicPowerStatus.Cancelled(world, facts, character, Statuses.Stunned, events))
        {
            return;
        }

        InitiateAttack(world, character, enemy, player);
    }

    /// <summary>Make a basic attack under permission not to exhaust.</summary>
    /// <remarks>
    /// <c>rr:attack-player-ability-type.1.2</c> explicitly permits the granting
    /// ability to use an exhausted hero or ally. This method is the permission
    /// boundary: ordinary basic attacks continue through <see cref="BasicAttack"/>
    /// and must pay their exhaust cost.
    /// </remarks>
    public static void BasicAttackWithoutExhausting(
        World world, ICardFacts facts, Card character, Card enemy,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(events);

        int player = character.Area.PlayArea.Player;
        var kind = EffectiveCards.Kind(character, facts);
        bool hero = IsActiveHero(world, facts, character, kind, player);
        bool ally = IsAlly(character, kind, player);
        if ((!hero && !ally) || !CanUsePower(facts, character, "ATK"))
        {
            throw new RulesNotImplementedException(
                $"card {character.ObjectId} cannot make the permitted basic attack");
        }

        if (!ValidTarget(world, facts, character, enemy, player))
        {
            throw new RulesNotImplementedException(
                $"card {enemy.ObjectId} is not an enemy card {character.ObjectId} can attack");
        }

        if (BasicAttackCosts.Schedule(world, character, enemy, exhaust: false)) return;
        CommitPaidAttack(world, facts, character, enemy, events);
    }

    internal static void CommitPaidAttack(World world, ICardFacts facts, Card character,
        Card enemy, List<GameEvent> events)
    {
        if (BasicPowerStatus.Cancelled(world, facts, character, Statuses.Stunned, events)) return;
        int player = CardControl.ControllerOf(world, character);
        InitiateAttack(world, character, enemy, player);
        if (EffectiveCards.Kind(character, facts) == CardKind.Ally)
            ScheduleConsequentialDamage(world, character, enemy, player);
    }

    private static bool IsActiveHero(
        World world, ICardFacts facts, Card card, CardKind kind, int player) =>
        kind == CardKind.Hero && player >= 0
        && world.Seats[player].IdentityCard.ObjectId == card.ObjectId
        && Forms.In(world, world.Seats[player], facts, Forms.Hero);

    private static bool IsAlly(Card card, CardKind kind, int player) =>
        kind == CardKind.Ally && card.Area.Type == DeckType.AlliesArea && player >= 0;

    private static bool ValidTarget(
        World world, ICardFacts facts, Card character, Card enemy, int player) =>
        Statuses.Afflicted(world, facts, character, Statuses.Stunned)
        || Attackable(world, facts, player, character).Any(card => card.ObjectId == enemy.ObjectId);

    private static void ScheduleConsequentialDamage(
        World world, Card character, Card enemy, int player) =>
        world.Agenda.Then(new PhaseStep(
            new ConsequentialDamageOperation(
                ConsequentialDamageKind.AllyConsequentialDamage,
                Character: enemy.ObjectId
            ) { PerformerIncarnation = character.Incarnation },
            world.Agenda.Current?.Round ?? 0,
            9,
            Index: player,
            Subject: character.ObjectId,
            Seat: player
        ));

    /// <summary>
    /// Puts a character's attack on the agenda —
    /// <c>rr:attack-player-ability-type</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scheduled rather than resolved.</b> <c>.step.7</c> and <c>.step.8</c>
    /// put abilities around the attack — "after [character] attacks [and
    /// damages/defeats] [an enemy/a minion]", "after [character] is attacked"
    /// — and one of them may ask the player something. The scheduled attack
    /// retains its actor, target and source through those windows.
    /// </para>
    /// <para>
    /// The cost is already paid by the time this runs:
    /// <c>rr:initiating-abilities.step.5</c> pays before step 6 resolves, and
    /// exhausting is the cost of a basic power.
    /// </para>
    /// </remarks>
    internal static void InitiateAttack(World world, Card attacker, Card enemy, int player) =>
        ScheduleAttack(world, new CharacterAttack(
            attacker.ObjectId, enemy.ObjectId, player, TargetIncarnation: enemy.Incarnation));

    private static void ScheduleAttack(World world, CharacterAttack attack, bool nested = false)
    {
        world.CharacterAttack = attack;
        var step = new PhaseStep(
            new CharacterAttackOperation(Attack: attack),
            world.Agenda.Current?.Round ?? 0,
            2,
            Index: attack.Player,
            Subject: attack.Enemy,
            Seat: attack.Player
        );
        if (nested) world.Agenda.Now(step);
        else world.Agenda.Then(step);
    }

    /// <summary>Initiates a card ability labelled as an attack.</summary>
    /// <remarks>
    /// The card has already paid its ability costs. A stun therefore replaces
    /// the attack without refunding those costs, and an attack that proceeds
    /// uses the same interrupt/response occurrence as a basic attack. The
    /// acting hero and damage source remain distinct because retaliate damages
    /// the former while damage prohibitions inspect the latter.
    /// </remarks>
    public static bool CardAttack(
        World world, ICardFacts facts, CardPowerInvocation power, List<GameEvent> events,
        bool overkill = false, Card? moveFrom = null, bool nested = false)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(power);
        ArgumentNullException.ThrowIfNull(events);

        var attacker = power.Performer ?? LabeledAbilities.Begin(
            world, facts, power.Player, power.Source, [AttackVerb], events);
        if (attacker is null) return false;

        RequireCardAttackTarget(world, facts, power, attacker);
        ScheduleAttack(world, new CharacterAttack(
            attacker.ObjectId, power.Target?.ObjectId ?? -1, power.Player, power.Amount,
            power.Source.ObjectId, moveFrom?.ObjectId ?? -1, overkill, power.Trigger,
            power.Continuation, power.Origin, power.SurgeGained, power.Target?.Incarnation ?? -1), nested);
        return true;
    }

    private static void RequireCardAttackTarget(World world, ICardFacts facts, CardPowerInvocation power, Card attacker)
    {
        // rr:labeled-ability.2 and ruling 0fc2a7cc9197e127 allow an attack
        // whose independent effect resolves without an attacked enemy. Only
        // an interpreter continuation can own that effect body.
        if (power.Target is null && power.Continuation is null)
            throw new RulesNotImplementedException("an enemy-less attack requires an interpreter continuation");
        if (power.Target is not null
            && (!Attackable(world, facts, power.Player, attacker).Any(card => card.ObjectId == power.Target.ObjectId)
                || !world.DamageAbilities.CanTakeDamage(world, power.Target, power.Source)))
        {
            throw new RulesNotImplementedException(
                $"card {power.Target.ObjectId} is not an enemy {world.Seats[power.Player].Name} can attack");
        }

        CardAttackTargets.Validate(world, facts, power, attacker);
    }

    /// <summary>
    /// Puts a character's thwart on the agenda — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scheduled rather than resolved</b>, for the reason
    /// <see cref="InitiateAttack"/> is. <c>rr:thwart</c> writes out no steps of
    /// its own, so the case comes from <c>rr:consequential-damage.1</c>: an
    /// ally's consequential damage is dealt "after resolving abilities that are
    /// triggered by the ally attacking <b>or thwarting</b>". Abilities triggered
    /// by a thwart resolve in its response window before consequential damage.
    /// </para>
    /// <para>
    /// The engine assigns number 3 to thwart and 2 to attack, so
    /// <c>Moment.Id</c> distinguishes their scheduled occurrences. The number
    /// is an engine identity choice, not a rulebook thwart-step number.
    /// </para>
    /// </remarks>
    internal static void InitiateThwart(World world, Card thwarter, Card scheme, int player) =>
        ScheduleThwart(world, new CharacterThwart(
            thwarter.ObjectId, scheme.ObjectId, player, TargetIncarnation: scheme.Incarnation));

    internal static void ScheduleThwart(World world, CharacterThwart thwart, bool nested = false)
    {
        world.CharacterThwart = thwart;
        var step = new PhaseStep(
            new CharacterThwartOperation(Thwart: thwart),
            world.Agenda.Current?.Round ?? 0,
            3,
            Index: thwart.Player,
            Subject: thwart.Scheme,
            Seat: thwart.Player
        );
        if (nested) world.Agenda.Now(step);
        else world.Agenda.Then(step);
    }
}
