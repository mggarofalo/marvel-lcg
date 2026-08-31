using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// The basic powers — <c>rr:basic-power</c>.
/// </summary>
/// <remarks>
/// <para>
/// "A basic power is a statistic that allows a character to perform a certain
/// game function", and <c>rr:basic-power.1</c> lists five. Three of them are
/// things a player <i>chooses</i> to do on their turn and are here:
/// <b>attack</b>, <b>thwart</b> and <b>recover</b>. Defence is the fourth and
/// belongs to an enemy's attack rather than to a turn — see
/// <see cref="Attack"/>. Scheme is the fifth and is an enemy's, not a
/// player's.
/// </para>
/// <para>
/// <c>rr:player-turn.3</c> is the gate: "use their alter-ego's basic recovery
/// <i>(if in alter-ego form)</i> or their hero's basic attack or thwart power
/// <i>(if in hero form)</i>." So which of the three is on offer is a question
/// about <see cref="Forms"/>, not about the card.
/// </para>
/// <para>
/// <b>The verb strings are on the wire.</b> The four basic powers are spelled
/// <c>Attack</c>, <c>Defense</c>, <c>Thwart</c> and <c>Recover</c> — the
/// rulebook's own names for them, capitalised. A client renders these, so they
/// are a contract and not a label: change one and every caller changes with it.
/// </para>
/// </remarks>
public static class BasicPowers
{
    /// <summary>Whether a character has a usable printed value for one basic power.</summary>
    public static bool CanUsePower(ICardFacts facts, Card character, string field)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentException.ThrowIfNullOrEmpty(field);

        return StateFields.HasUsablePrintedPower(facts, character.FaceId, field);
    }

    /// <summary>The affordance verb for a basic attack.</summary>
    public const string AttackVerb = "Attack";

    /// <summary>The affordance verb for a basic thwart.</summary>
    public const string ThwartVerb = "Thwart";

    /// <summary>The affordance verb for a basic recovery.</summary>
    public const string RecoverVerb = "Recover";

    /// <summary>
    /// The enemies this player's character may attack right now.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:attack-player-ability-type.4</c>: "hero and ally attacks can
    /// target <b>any</b> enemy, unless a card ability <i>(such as guard)</i> is
    /// preventing that enemy from being attacked."
    /// </para>
    /// <para>
    /// <c>rr:guard.1</c> is that ability written out: "the engaged player
    /// cannot attack any villain." So a minion with guard engaged with this
    /// player removes every villain from the list and leaves the minions —
    /// including itself.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="player">Who is attacking.</param>
    public static IReadOnlyList<Card> Attackable(World world, ICardFacts facts, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        var enemies = Enemies(world, facts);
        bool guarded = enemies.Any(enemy =>
            FacedownDrones.Kind(enemy, facts) == CardKind.Minion
            && Engaged(world, enemy) == player
            && StateFields.Modified(world, enemy, "guard", facts, world.Players) > 0);

        return guarded
            ? [.. enemies.Where(enemy => FacedownDrones.Kind(enemy, facts) == CardKind.Minion)]
            : enemies;
    }

    /// <summary>
    /// The schemes this player's character may thwart right now.
    /// </summary>
    /// <remarks>
    /// <c>rr:thwart.1.1</c>: "a character can only initiate a basic thwart if
    /// there is a scheme with <b>at least one threat</b> for the character to
    /// remove." A scheme at zero threat is not a legal target, which is not the
    /// same as thwarting it for no effect.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="player">Who is thwarting — <c>rr:patrol</c> is theirs.</param>
    public static IReadOnlyList<Card> Thwartable(World world, ICardFacts facts, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        // **Asked before the loop, not inside it.** `World.AreaOf` makes the
        // area when there is none, so calling it while iterating `world.Areas`
        // can add to the list being walked -- which it did, and the exception
        // was the only warning.
        //
        // `rr:patrol.1`, as a constant ability: "the engaged player cannot
        // thwart the main scheme." The main scheme only -- a side scheme is
        // still fair game, which is the difference from `rr:guard`.
        bool patrolled = Patrolled(world, facts, player);

        // `rr:crisis-icon.1`, as a constant ability: "player cards cannot remove
        // threat from the main scheme." A hero's identity and an ally are both
        // player cards, so a crisis icon anywhere in play takes the main scheme
        // off everybody's list -- unlike `rr:patrol`, which is one player's.
        // `.2` exempts encounter card abilities, which do not come through here.
        bool crisis = MainScheme.Crisis(world, facts);

        var schemes = new List<Card>();
        foreach (var area in world.Areas)
        {
            if (area.Type is not (DeckType.MainSchemesArea or DeckType.SideSchemesArea))
            {
                continue;
            }

            if (area.Type == DeckType.MainSchemesArea && (patrolled || crisis))
            {
                continue;
            }

            schemes.AddRange(area.Cards.Where(scheme =>
                scheme.Tokens.GetValueOrDefault("k_threat") > 0
                && world.Abilities.CanRemoveThreat(world, scheme)));
        }

        return schemes;
    }

    /// <summary>
    /// Whether a player may perform a basic recovery — <c>rr:recover-recovery</c>.
    /// </summary>
    /// <remarks>
    /// "Recovery is a basic power a player can use <b>in alter-ego form</b>. To
    /// recover, the player exhausts their alter-ego and heals a number of hit
    /// points equal to their REC value." And <c>rr:recover-recovery.1</c>: "an
    /// identity that has <b>no damage to heal</b> cannot perform a basic
    /// recovery."
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="player">Who would recover.</param>
    public static bool CanRecover(World world, ICardFacts facts, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        var seat = world.Seats[player];
        return Forms.In(world, seat, facts, Forms.AlterEgo)
            && seat.IdentityCard.Ready
            && CanUsePower(facts, seat.IdentityCard, "REC")
            && seat.IdentityCard.Damage > 0;
    }

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
        if (Cancelled(world, facts, character, Statuses.Stunned, events))
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
        bool hero = kind == CardKind.Hero
            && player >= 0
            && world.Seats[player].IdentityCard.ObjectId == character.ObjectId
            && Forms.In(world, world.Seats[player], facts, Forms.Hero);
        bool ally = kind == CardKind.Ally
            && character.Area.Type == DeckType.AlliesArea
            && player >= 0;
        if ((!hero && !ally) || !CanUsePower(facts, character, "ATK"))
        {
            throw new RulesNotImplementedException(
                $"card {character.ObjectId} cannot make the permitted basic attack");
        }

        if (!Statuses.Afflicted(world, facts, character, Statuses.Stunned)
            && !Attackable(world, facts, player).Any(card => card.ObjectId == enemy.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"card {enemy.ObjectId} is not an enemy card {character.ObjectId} can attack");
        }

        if (Cancelled(world, facts, character, Statuses.Stunned, events))
        {
            return;
        }

        InitiateAttack(world, character, enemy, player);
        if (ally)
        {
            world.Agenda.Then(new PhaseStep(
                Steps.AllyConsequentialDamage,
                world.Agenda.Current?.Round ?? 0,
                9,
                Index: player,
                Subject: character.ObjectId,
                Seat: player,
                Character: enemy.ObjectId));
        }
    }

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
    private static void InitiateAttack(
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
            || !world.Abilities.CanTakeDamage(world, enemy, source))
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
    private static void InitiateThwart(
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

    /// <summary>
    /// Takes the thwart's threat off — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// "This removes threat equal to the character's THW value from the
    /// scheme." The mirror of <see cref="ResolveCharacterAttack"/>, and the
    /// place a scheme thwarted to zero is defeated from.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="scheduled">The thwart carried by the current agenda step.</param>
    public static void ResolveCharacterThwart(
        World world, ICardFacts facts, List<GameEvent> events,
        CharacterThwart? scheduled = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        var thwart = scheduled ?? world.CharacterThwart;
        if (thwart is null)
        {
            throw new RulesNotImplementedException(
                "a character thwart is resolving and the board holds none");
        }

        if (thwart.AbilityIndex < 0
            && (!DeckTypes.IsInPlay(world.Cards[thwart.Scheme].Area.Type)
                || world.Cards[thwart.Scheme].Incarnation != thwart.TargetIncarnation))
        {
            world.Agenda.CancelConsequentialDamage(
                thwart.Thwarter, thwart.Scheme, attack: false);
            return;
        }

        if (thwart.AbilityIndex >= 0)
        {
            var occurrence = world.Agenda.Occurrence
                ?? throw new RulesNotImplementedException(
                    "a character thwart resolved without an occurrence for its response window");
            world.Abilities.ResolveCardThwart(world, thwart, occurrence, events);
            return;
        }

        if (thwart.Amount >= 0)
        {
            Threat.Remove(
                world,
                facts,
                world.Abilities,
                world.Cards[thwart.Scheme],
                thwart.Amount,
                thwart.Trigger,
                ThwartVerb,
                events,
                thwart.Player);
            return;
        }

        RemoveThreat(
            world, facts, world.Cards[thwart.Thwarter], world.Cards[thwart.Scheme], events);
    }

    /// <summary>
    /// Deals the attack's damage — <c>rr:attack-player-ability-type.1</c>.
    /// </summary>
    /// <remarks>
    /// "This deals damage equal to the character's ATK value to the enemy."
    /// Through <see cref="Damage"/>'s attack primitive, which is the same one an
    /// enemy's attack uses: <c>rr:damage</c> is one rule however the damage
    /// arrived.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="scheduled">The attack carried by the current agenda step.</param>
    public static void ResolveCharacterAttack(
        World world, ICardFacts facts, List<GameEvent> events,
        CharacterAttack? scheduled = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        var attack = scheduled ?? world.CharacterAttack;
        if (attack is null)
        {
            throw new RulesNotImplementedException(
                "a character attack is resolving and the board holds none");
        }

        var occurrence = world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "a character attack resolved without an occurrence for its response window");
        if (attack.AbilityIndex < 0
            && (!DeckTypes.IsInPlay(world.Cards[attack.Enemy].Area.Type)
                || world.Cards[attack.Enemy].Incarnation != attack.TargetIncarnation))
        {
            // `rr:consequential-damage.2` and `.2.1`: if an ally's basic-power
            // target leaves before application, the exhausted ally neither
            // attacked nor takes consequential damage. No AttackEnds condition
            // is added because the attack itself aborted.
            world.Agenda.CancelConsequentialDamage(
                attack.Attacker, attack.Enemy, attack: true);
            return;
        }
        if (attack.AbilityIndex >= 0)
        {
            world.Abilities.ResolveCardAttack(world, attack, occurrence, events);
            occurrence.Also(Steps.AttackEnds);
            return;
        }

        var attacker = world.Cards[attack.Attacker];
        var source = attack.Source >= 0 ? world.Cards[attack.Source] : attacker;
        long amount = attack.Amount >= 0
            ? attack.Amount
            : StateFields.Modified(world, attacker, "attack", facts, world.Players);

        ContinuousEffect? temporaryOverkill = null;
        if (attack.Overkill)
        {
            temporaryOverkill = new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: Keywords.Overkill,
                Amount: 1,
                Card: source.ObjectId,
                Affects: attacker.ObjectId,
                Lasts: new Duration(Uses: 1));
            world.Effects.Register(temporaryOverkill);
        }

        if (attack.MoveFrom >= 0)
        {
            var from = world.Cards[attack.MoveFrom];
            amount = Math.Min(amount, from.Damage);
            if (amount > 0 && world.Abilities.CanTakeDamage(world, world.Cards[attack.Enemy], source))
            {
                Damage.Heal(world, facts, from, amount, attack.Trigger, "Move_Damage", events);
            }
            else
            {
                amount = 0;
            }
        }

        var damaged = Damage.Attack(
            world, facts, attacker, source, world.Cards[attack.Enemy], amount,
            attack.Trigger, AttackVerb, events);

        if (temporaryOverkill is not null)
        {
            world.Effects.Use(temporaryOverkill);
        }

        occurrence.Also(Steps.AttackEnds);
        if (damaged.Characters.Count > 0)
        {
            occurrence.Also(Steps.DamageDealt);
        }
    }

    /// <summary>
    /// An ally's consequential damage —
    /// <c>rr:attack-player-ability-type.step.9</c>.
    /// </summary>
    /// <remarks>
    /// The icons sit under the field that was used, as stars inside
    /// <c>ATK</c>/<c>THW</c> rather than an attribute of their own — so the
    /// printed half is read and the modified half is looked up, the same split
    /// as <c>Damage.Health</c>.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="ally">The ally that attacked or thwarted.</param>
    /// <param name="byAttack">Whether the field used was <c>ATK</c>.</param>
    /// <param name="verb">What the ally did.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Consequential(
        World world, ICardFacts facts, Card ally, bool byAttack, string verb,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(ally);
        ArgumentNullException.ThrowIfNull(events);

        long consequential =
            facts.ConsequentialDamage(ally.FaceId, byAttack ? "ATK" : "THW")
            + StateFields.Modified(
                world, ally,
                byAttack ? "attack_consequential_damage" : "thwart_consequential_damage",
                facts, world.Players);

        Damage.Deal(
            world, facts, ally, ally, consequential, verb, "Consequential_Damage", events);
    }

    /// <summary>
    /// A basic thwart — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// "A hero or ally can use their basic thwart power to thwart a scheme. A
    /// character <b>must exhaust</b> to use this power. This removes threat
    /// equal to the character's THW value from the scheme."
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="player">Who is thwarting.</param>
    /// <param name="scheme">Which scheme.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void BasicThwart(
        World world, ICardFacts facts, int player, Card scheme, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        var seat = world.Seats[player];
        var character = seat.IdentityCard;
        Require(
            world, facts, seat, Forms.Hero, ThwartVerb,
            UsesAttack(world, facts, scheme) ? "ATK" : "THW");

        // `rr:thwart.1.1` and `rr:confuse-confused.5.1`, the same pair.
        if (!Statuses.Afflicted(world, facts, character, Statuses.Confused)
            && !Thwartable(world, facts, player).Any(t => t.ObjectId == scheme.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"card {scheme.ObjectId} is not a scheme {seat.Name} can thwart");
        }

        Exhaust(character, ThwartVerb, events);

        // `rr:confuse-confused.1` and `.5`, the thwart's half of the same rule.
        if (Cancelled(world, facts, character, Statuses.Confused, events))
        {
            return;
        }

        InitiateThwart(world, character, scheme, player);
    }

    /// <summary>Initiates a card ability labelled as a thwart.</summary>
    public static bool CardThwart(
        World world, ICardFacts facts, int player, Card source, Card scheme, long amount,
        string trigger, List<GameEvent> events, int abilityIndex = -1,
        int powerOrdinal = 0, int resumeFrom = -1,
        bool finalStep = false, IReadOnlyList<int>? targets = null,
        ThreatPlacement? imminentThreat = null, bool automaticTarget = false,
        bool nested = false, bool surgeGained = false,
        IReadOnlyList<string>? abilityPath = null, string abilityFace = "",
        IReadOnlyDictionary<string, long>? abilityResults = null,
        Occurrence? abilityOccurrence = null, IReadOnlyList<int>? discarded = null,
        bool eachPlayerFrame = false, bool finalPlayer = false, int abilityPlayer = -1,
        bool abilityHasContinuation = false, Card? performer = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        var thwarter = performer ?? LabeledAbilities.Begin(
            world, facts, player, source, [ThwartVerb], events);
        if (thwarter is null)
        {
            return false;
        }

        if (automaticTarget
            ? !CanAutomaticallyThwart(world, facts, player, scheme)
            : !Thwartable(world, facts, player).Any(card => card.ObjectId == scheme.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"card {scheme.ObjectId} is not a scheme {world.Seats[player].Name} can thwart");
        }

        InitiateThwart(
            world, thwarter, scheme, player, amount, source, trigger, abilityIndex,
            powerOrdinal, resumeFrom, finalStep, targets, imminentThreat, nested,
            surgeGained, abilityPath, abilityFace, abilityResults, abilityOccurrence,
            discarded, eachPlayerFrame, finalPlayer, abilityPlayer,
            abilityHasContinuation, thwarter.ObjectId);

        return true;
    }

    /// <summary>Whether a card's already-determined scheme may be thwarted.</summary>
    /// <remarks>
    /// Crisis prohibits removing threat, so it does not prohibit Emergency
    /// from preventing imminent threat. Patrol instead says the engaged player
    /// cannot thwart the main scheme at all, and therefore still applies.
    /// </remarks>
    public static bool CanAutomaticallyThwart(
        World world, ICardFacts facts, int player, Card scheme)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(scheme);

        bool isScheme = scheme.Area.Type is
            DeckType.MainSchemesArea or DeckType.SideSchemesArea;
        return isScheme
            && (scheme.Area.Type != DeckType.MainSchemesArea
                || !Patrolled(world, facts, player));
    }

    /// <summary>Whether a basic thwart against this scheme uses ATK.</summary>
    /// <remarks>
    /// <c>rr:assault.1</c>, as a constant ability: "while a character is making
    /// a basic thwart against this scheme, that character uses its ATK instead
    /// of its THW."
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="scheme">The scheme being thwarted.</param>
    public static bool UsesAttack(World world, ICardFacts facts, Card scheme)
    {
        ArgumentNullException.ThrowIfNull(world);
        return Assaulted(world, facts, scheme);
    }

    /// <summary>
    /// A basic recovery — <c>rr:recover-recovery</c>.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="player">Who is recovering.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void BasicRecovery(
        World world, ICardFacts facts, int player, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        var seat = world.Seats[player];
        Require(world, facts, seat, Forms.AlterEgo, RecoverVerb, "REC");

        if (seat.IdentityCard.Damage == 0)
        {
            throw new RulesNotImplementedException(
                $"{seat.Name} has no damage to heal, and rr:recover-recovery.1 does not "
                + "permit a basic recovery");
        }

        Exhaust(seat.IdentityCard, RecoverVerb, events);
        Damage.Heal(
            world, facts, seat.IdentityCard,
            StateFields.Modified(world, seat.IdentityCard, "recover", facts, world.Players),
            RecoverVerb, RecoverVerb, events);
    }

    /// <summary>
    /// The allies a player may use to attack or thwart — <c>rr:ally.2</c>.
    /// </summary>
    /// <remarks>
    /// "During a player's turn, they may use <b>any number</b> of allies they
    /// control to attack or thwart. An ally <b>must exhaust</b> to attack or
    /// thwart." Any number, so this is every ready one and not a choice of one.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="player">Whose allies.</param>
    public static IReadOnlyList<Card> Allies(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        return [.. world
            .AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player)
            .Cards
            .Where(ally => ally.Ready)];
    }

    /// <summary>
    /// An ally attacks or thwarts — <c>rr:ally.2</c> and <c>rr:ally.3</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same two powers a hero has, with two differences the rules state
    /// outright. <c>rr:ally.5</c>: an ally's attack "is <b>not</b> considered to
    /// be performed by that player's identity", so the form gate in
    /// <c>rr:player-turn.3</c> does not apply — an ally can attack while its
    /// controller is in alter-ego form.
    /// </para>
    /// <para>
    /// <c>rr:ally.3</c> is the other: "after an ally is used to attack or
    /// thwart, deal consequential damage to that ally equal to the number of
    /// consequential damage icons beneath the ally's ATK or THW field". A hero
    /// takes none — <c>rr:consequential-damage</c> is an ally rule.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="ally">Which ally.</param>
    /// <param name="target">The enemy or scheme.</param>
    /// <param name="verb">Whether this is an attack or a thwart.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void AllyPower(
        World world, ICardFacts facts, Card ally, Card target, string verb,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(ally);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        if (!ally.Ready)
        {
            throw new RulesNotImplementedException(
                $"card {ally.ObjectId} is exhausted and must exhaust to {verb}");
        }

        bool attacking = string.Equals(verb, AttackVerb, StringComparison.Ordinal);
        string field = attacking ? "ATK" : "THW";
        if (!CanUsePower(facts, ally, field))
        {
            throw new RulesNotImplementedException(
                $"card {ally.ObjectId} has no usable {field} value");
        }

        // A status-cancelled attempt needs no valid target
        // (`rr:stun-stunned.5.1`, `rr:confuse-confused.5.1`). Check it before
        // target legality so an ally can clear the status when the legal set
        // is empty, and take no consequential damage (`rr:ally.3`).
        string cancellingStatus = attacking ? Statuses.Stunned : Statuses.Confused;
        if (Statuses.Afflicted(world, facts, ally, cancellingStatus))
        {
            Exhaust(ally, verb, events);
            Cancelled(world, facts, ally, cancellingStatus, events);
            return;
        }

        var legal = attacking
            ? Attackable(world, facts, ally.Owner)
            : Thwartable(world, facts, ally.Owner);

        if (!legal.Any(option => option.ObjectId == target.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"card {target.ObjectId} is not something card {ally.ObjectId} can {verb}");
        }

        Exhaust(ally, verb, events);

        // Scheduled, both halves -- and the consequential damage after the
        // power, because `rr:consequential-damage.1` deals it "after resolving
        // abilities that are triggered by the ally attacking or thwarting".
        // Dealt inline it would land before the enemy that answers "after this
        // is attacked" had spoken.
        //
        // `rr:attack-player-ability-type.step.9` says the same thing from the
        // attack's side, and puts it last of all: after `.step.7`'s forced
        // abilities and `.step.8`'s optional ones.
        if (attacking)
        {
            InitiateAttack(world, ally, target, ally.Owner);
        }
        else
        {
            InitiateThwart(world, ally, target, ally.Owner);
        }

        world.Agenda.Then(new PhaseStep(
            attacking ? Steps.AllyConsequentialDamage : Steps.AllyThwartConsequentialDamage,
            world.Agenda.Current?.Round ?? 0,
            9,
            Index: ally.Owner,
            Subject: ally.ObjectId,
            Seat: ally.Owner,
            Character: target.ObjectId));
    }

    /// <summary>Pay for a status-cancelled basic power with no legal target.</summary>
    internal static void CancelledBasicPower(
        World world, ICardFacts facts, Card character, string verb,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(events);

        bool attacking = string.Equals(verb, AttackVerb, StringComparison.Ordinal);
        string field = attacking ? "ATK" : "THW";
        string status = attacking ? Statuses.Stunned : Statuses.Confused;
        if (!character.Ready || !CanUsePower(facts, character, field)
            || !Statuses.Afflicted(world, facts, character, status))
        {
            throw new RulesNotImplementedException(
                $"card {character.ObjectId} cannot make a targetless {verb} attempt");
        }

        Exhaust(character, verb, events);
        if (!Cancelled(world, facts, character, status, events))
        {
            throw new InvalidOperationException(
                $"card {character.ObjectId}'s targetless {verb} was not cancelled");
        }
    }

    /// <summary>
    /// Whether a minion with <c>rr:patrol</c> is engaged with this player.
    /// </summary>
    private static bool Patrolled(World world, ICardFacts facts, int player) =>
        world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)).Cards
            .Any(minion => StateFields.Modified(
                world, minion, "patrol", facts, world.Players) > 0);

    /// <summary>Whether a scheme carries <c>rr:assault</c>.</summary>
    internal static bool Assaulted(World world, ICardFacts facts, Card scheme) =>
        StateFields.Modified(world, scheme, "assault", facts, world.Players) > 0;

    /// <summary>
    /// A status card cancels the action it names — <c>rr:stun-stunned.1</c>,
    /// <c>rr:confuse-confused.1</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "<b>Forced Interrupt</b>: when this character would attack, remove
    /// <b>each</b> stunned status card from it instead." Each, not one — which
    /// is the opposite of <c>rr:tough.2.1</c>, and <c>rr:steady</c> is what
    /// makes the difference visible: a steady character carrying two loses both.
    /// </para>
    /// <para>
    /// <c>rr:stun-stunned.5</c>: "costs associated with the attack attempt,
    /// <b>including exhausting the character</b>, must still be paid." So the
    /// caller exhausts first and asks this second.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="character">Who is acting.</param>
    /// <param name="status">The status that would cancel it.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>Whether the action was cancelled.</returns>
    public static bool Cancelled(
        World world, ICardFacts facts, Card character, string status, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(events);

        if (!Statuses.Afflicted(world, facts, character, status))
        {
            return false;
        }

        foreach (var card in Statuses.On(world, character, status).ToList())
        {
            Discard.Card(world, card, status, events);
        }

        return true;
    }

    /// <summary>The seat a minion is engaged with, or -1.</summary>
    private static int Engaged(World world, Card enemy) =>
        enemy.Area.Type == DeckType.EngagedEnemiesArea ? enemy.Area.PlayArea.Player : -1;

    /// <summary>Every villain and minion in play.</summary>
    private static List<Card> Enemies(World world, ICardFacts facts)
    {
        var enemies = new List<Card>();
        foreach (var area in world.Areas)
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            // `rr:enemy`: "an enemy is a minion or villain."
            enemies.AddRange(area.Cards.Where(card =>
                CardKinds.IsEnemy(FacedownDrones.Kind(card, facts))));
        }

        return enemies;
    }

    /// <summary>`rr:player-turn.3` -- which powers a form permits.</summary>
    private static void Require(
        World world, ICardFacts facts, Seat seat, string form, string verb, string field)
    {
        if (!Forms.In(world, seat, facts, form))
        {
            throw new RulesNotImplementedException(
                $"{seat.Name} is not in {form} form, and rr:player-turn.3 permits a basic "
                + $"{verb} only in that form");
        }

        if (!seat.IdentityCard.Ready)
        {
            // `rr:exhausted.2`: "if an exhausted card must exhaust to pay the
            // cost of using its ability, that ability cannot be used until the
            // card is ready."
            throw new RulesNotImplementedException(
                $"{seat.Name} is exhausted and a basic {verb} must exhaust to use");
        }

        if (!CanUsePower(facts, seat.IdentityCard, field))
        {
            throw new RulesNotImplementedException(
                $"{seat.Name}'s {field} value is a dash and cannot be used");
        }
    }

    /// <summary>
    /// A character's THW comes off a scheme — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// Capped by the threat that is there: <c>rr:threat</c> counts tokens, and
    /// a scheme cannot hold a negative number of them. <c>Card.PlaceTokens</c>
    /// clamps too, so without this the board would be right and the event would
    /// report a scheme going below zero.
    /// </remarks>
    private static void RemoveThreat(
        World world, ICardFacts facts, Card character, Card scheme, List<GameEvent> events)
    {
        string power = Assaulted(world, facts, scheme) ? "attack" : "thwart";
        // Who did it, for the cards that ask. `rr:ownership-and-control.2`
        // puts a card under its owner's control, so an ally's thwart is still
        // its owner's doing -- `rr:you-your.15` keeps it off that player's
        // identity, which is a different question.
        Threat.Remove(
            world, facts, world.Abilities, scheme,
            StateFields.Modified(world, character, power, facts, world.Players),
            ThwartVerb, ThwartVerb, events, by: character.Owner);
    }

    private static void Exhaust(Card character, string verb, List<GameEvent> events)
    {
        character.Exhaust();
        events.Add(new FieldSet(character.ObjectId, "is_exhaust", 0, 1)
        {
            Trigger = verb, Verb = verb,
        });
    }
}
