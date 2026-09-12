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
        if (!Statuses.Afflicted(world, facts, character, Statuses.Stunned)
            && !Attackable(world, facts, player).Any(t => t.ObjectId == enemy.ObjectId))
        {
            // `rr:guard.1` is the case this catches in an ordinary game: a
            // minion with guard engaged with this player makes every villain an
            // illegal target while it is in play.
            throw new RulesNotImplementedException(
                $"card {enemy.ObjectId} is not an enemy {seat.Name} can attack");
        }

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
        var kind = FacedownDrones.Kind(character, facts);
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

        if (BasicPowerStatus.Cancelled(world, facts, character, Statuses.Stunned, events))
        {
            return;
        }

        InitiateAttack(world, character, enemy, player);
        if (ally) ScheduleConsequentialDamage(world, character, enemy, player);
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
        || Attackable(world, facts, player).Any(card => card.ObjectId == enemy.ObjectId);

    private static void ScheduleConsequentialDamage(
        World world, Card character, Card enemy, int player) =>
        world.Agenda.Then(new PhaseStep(
            Steps.AllyConsequentialDamage, world.Agenda.Current?.Round ?? 0, 9,
            Index: player, Subject: character.ObjectId, Seat: player,
            Character: enemy.ObjectId));

    /// <summary>
    /// Puts a character's attack on the agenda —
    /// <c>rr:attack-player-ability-type</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scheduled rather than resolved.</b> <c>.step.7</c> and <c>.step.8</c>
    /// put abilities around the attack — "after [character] attacks [and
    /// damages/defeats] [an enemy/a minion]", "after [character] is attacked"
    /// — and one of them may ask the player something. A basic attack that
    /// dealt its damage inline had nowhere to open those windows, which is why
    /// Shocker's "after Shocker is attacked" could not be written.
    /// </para>
    /// <para>
    /// The cost is already paid by the time this runs:
    /// <c>rr:initiating-abilities.step.5</c> pays before step 6 resolves, and
    /// exhausting is the cost of a basic power.
    /// </para>
    /// </remarks>
    internal static void InitiateAttack(
        World world, Card attacker, Card enemy, int player, long amount = -1,
        Card? source = null, Card? moveFrom = null, bool overkill = false,
        string trigger = AttackVerb, int abilityIndex = -1, int powerOrdinal = 0,
        int resumeFrom = -1,
        bool finalStep = false, IReadOnlyList<int>? targets = null, bool nested = false,
        bool surgeGained = false, IReadOnlyList<string>? abilityPath = null,
        string abilityFace = "", IReadOnlyDictionary<string, long>? abilityResults = null,
        Occurrence? abilityOccurrence = null, IReadOnlyList<int>? discarded = null,
        bool eachPlayerFrame = false, bool finalPlayer = false, int abilityPlayer = -1,
        bool abilityHasContinuation = false, int abilityActor = -1)
    {
        var attack = new CharacterAttack(
            attacker.ObjectId,
            enemy.ObjectId,
            player,
            amount,
            source?.ObjectId ?? -1,
            moveFrom?.ObjectId ?? -1,
            overkill,
            trigger,
            abilityIndex,
            powerOrdinal,
            resumeFrom,
            finalStep,
            targets,
            surgeGained,
            abilityPath,
            abilityFace,
            abilityResults,
            abilityOccurrence,
            discarded,
            eachPlayerFrame,
            finalPlayer,
            abilityPlayer,
            abilityActor,
            abilityHasContinuation,
            enemy.Incarnation);
        world.CharacterAttack = attack;
        var step = new PhaseStep(
            Steps.CharacterAttacks,
            world.Agenda.Current?.Round ?? 0,
            2,
            Index: player,
            Subject: enemy.ObjectId,
            Seat: player,
            CharacterAttack: attack,
            SurgeGained: surgeGained);
        if (nested)
        {
            world.Agenda.Now(step);
        }
        else
        {
            world.Agenda.Then(step);
        }
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
        World world, ICardFacts facts, int player, Card source, Card enemy, long amount,
        string trigger, List<GameEvent> events, bool overkill = false, Card? moveFrom = null,
        int abilityIndex = -1, int powerOrdinal = 0, int resumeFrom = -1,
        bool finalStep = false,
        IReadOnlyList<int>? targets = null, bool nested = false,
        bool surgeGained = false, IReadOnlyList<string>? abilityPath = null,
        string abilityFace = "", IReadOnlyDictionary<string, long>? abilityResults = null,
        Occurrence? abilityOccurrence = null, IReadOnlyList<int>? discarded = null,
        bool eachPlayerFrame = false, bool finalPlayer = false, int abilityPlayer = -1,
        bool abilityHasContinuation = false, Card? performer = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(events);

        var attacker = performer ?? LabeledAbilities.Begin(
            world, facts, player, source, [AttackVerb], events);
        if (attacker is null)
        {
            return false;
        }

        if (!Attackable(world, facts, player).Any(card => card.ObjectId == enemy.ObjectId)
            || !world.DamageAbilities.CanTakeDamage(world, enemy, source))
        {
            throw new RulesNotImplementedException(
                $"card {enemy.ObjectId} is not an enemy {world.Seats[player].Name} can attack");
        }

        InitiateAttack(
            world, attacker, enemy, player, amount, source, moveFrom, overkill, trigger,
            abilityIndex, powerOrdinal, resumeFrom, finalStep, targets, nested, surgeGained,
            abilityPath, abilityFace, abilityResults, abilityOccurrence, discarded,
            eachPlayerFrame, finalPlayer, abilityPlayer, abilityHasContinuation,
            attacker.ObjectId);
        return true;
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
    /// by a thwart are abilities in the window after it, and a thwart that took
    /// its threat off inline had nowhere to open one.
    /// </para>
    /// <para>
    /// The number is 3 where the attack's is 2 so that <c>Moment.Id</c> tells
    /// the two apart, and <b>nothing yet reads that number</b>: an occurrence
    /// remembers which abilities have used it in a set of its own, so what
    /// distinguishes two occurrences at run time is that they are two objects.
    /// Giving a thwart the attack's number changes no behaviour today. It is
    /// written correctly anyway because the id is what a saved game would have
    /// to rebuild an occurrence from, and a number that was already wrong when
    /// nothing read it would be wrong on the day something did.
    /// </para>
    /// </remarks>
    internal static void InitiateThwart(
        World world, Card thwarter, Card scheme, int player, long amount = -1,
        Card? source = null, string trigger = ThwartVerb, int abilityIndex = -1,
        int powerOrdinal = 0, int resumeFrom = -1, bool finalStep = false,
        IReadOnlyList<int>? targets = null,
        ThreatPlacement? imminentThreat = null, bool nested = false,
        bool surgeGained = false, IReadOnlyList<string>? abilityPath = null,
        string abilityFace = "", IReadOnlyDictionary<string, long>? abilityResults = null,
        Occurrence? abilityOccurrence = null, IReadOnlyList<int>? discarded = null,
        bool eachPlayerFrame = false, bool finalPlayer = false, int abilityPlayer = -1,
        bool abilityHasContinuation = false, int abilityActor = -1)
    {
        var thwart = new CharacterThwart(
            thwarter.ObjectId,
            scheme.ObjectId,
            player,
            amount,
            source?.ObjectId ?? -1,
            trigger,
            abilityIndex,
            powerOrdinal,
            resumeFrom,
            finalStep,
            targets,
            imminentThreat,
            surgeGained,
            abilityPath,
            abilityFace,
            abilityResults,
            abilityOccurrence,
            discarded,
            eachPlayerFrame,
            finalPlayer,
            abilityPlayer,
            abilityActor,
            abilityHasContinuation,
            scheme.Incarnation);
        world.CharacterThwart = thwart;
        var step = new PhaseStep(
            Steps.CharacterThwarts,
            world.Agenda.Current?.Round ?? 0,
            3,
            Index: player,
            Subject: scheme.ObjectId,
            Seat: player,
            CharacterThwart: thwart,
            SurgeGained: surgeGained);
        if (nested)
        {
            world.Agenda.Now(step);
        }
        else
        {
            world.Agenda.Then(step);
        }
    }
}
