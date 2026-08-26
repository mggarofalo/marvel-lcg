using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

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
/// <b>The verb strings are on the wire.</b> The oracle's
/// <c>Effect.GetDisplayName</c> names the four basic powers <c>Attack</c>,
/// <c>Defense</c>, <c>Thwart</c> and <c>Recover</c>, and
/// <c>datasets/digest/prompts.json</c> is there to check the half of the return
/// value they appear in.
/// </para>
/// </remarks>
public static class BasicPowers
{
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
            facts.Kind(enemy.FaceId) == CardKind.Minion
            && Engaged(world, enemy) == player
            && StateFields.Modified(world, enemy, "guard", facts, world.Players) > 0);

        return guarded
            ? [.. enemies.Where(enemy => facts.Kind(enemy.FaceId) == CardKind.Minion)]
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
    public static IReadOnlyList<Card> Thwartable(World world, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        var schemes = new List<Card>();
        foreach (var area in world.Areas)
        {
            if (area.Type is not (DeckType.MainSchemesArea or DeckType.SideSchemesArea))
            {
                continue;
            }

            schemes.AddRange(area.Cards.Where(
                scheme => scheme.Tokens.GetValueOrDefault("k_threat") > 0));
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
        Require(world, facts, seat, Forms.Hero, AttackVerb);

        if (!Attackable(world, facts, player).Any(target => target.ObjectId == enemy.ObjectId))
        {
            // `rr:guard.1` is the case this catches in an ordinary game: a
            // minion with guard engaged with this player makes every villain an
            // illegal target while it is in play.
            throw new RulesNotImplementedException(
                $"card {enemy.ObjectId} is not an enemy {seat.Name} can attack");
        }

        Exhaust(character, AttackVerb, events);
        Damage.Deal(
            world, facts, enemy,
            StateFields.Modified(world, character, "attack", facts, world.Players),
            AttackVerb, AttackVerb, events);
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
        Require(world, facts, seat, Forms.Hero, ThwartVerb);

        if (!Thwartable(world, facts).Any(target => target.ObjectId == scheme.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"card {scheme.ObjectId} is not a scheme {seat.Name} can thwart");
        }

        Exhaust(character, ThwartVerb, events);
        RemoveThreat(world, facts, character, scheme, events);
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
        Require(world, facts, seat, Forms.AlterEgo, RecoverVerb);

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
        var legal = attacking
            ? Attackable(world, facts, ally.Owner)
            : Thwartable(world, facts);

        if (!legal.Any(option => option.ObjectId == target.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"card {target.ObjectId} is not something card {ally.ObjectId} can {verb}");
        }

        Exhaust(ally, verb, events);

        if (attacking)
        {
            Damage.Deal(
                world, facts, target,
                StateFields.Modified(world, ally, "attack", facts, world.Players),
                verb, verb, events);
        }
        else
        {
            RemoveThreat(world, facts, ally, target, events);
        }

        // `rr:consequential-damage.1`: dealt "after resolving abilities that are
        // triggered by the ally attacking or thwarting", so after the attack and
        // not as part of it. The icons sit under the field that was used.
        // The printed icons, plus anything modifying the field. The icons are
        // stars inside `ATK`/`THW` rather than an attribute of their own, so
        // the printed half is read and the modified half is looked up -- the
        // same split as `Damage.Health`.
        long consequential =
            facts.ConsequentialDamage(ally.FaceId, attacking ? "ATK" : "THW")
            + StateFields.Modified(
                world, ally,
                attacking ? "attack_consequential_damage" : "thwart_consequential_damage",
                facts, world.Players);

        Damage.Deal(world, facts, ally, consequential, verb, "Consequential_Damage", events);
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
                facts.Kind(card.FaceId) is CardKind.EncounterVillain or CardKind.Minion));
        }

        return enemies;
    }

    /// <summary>`rr:player-turn.3` -- which powers a form permits.</summary>
    private static void Require(
        World world, ICardFacts facts, Seat seat, string form, string verb)
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
        long held = scheme.Tokens.GetValueOrDefault("k_threat");
        long removed = Math.Min(
            held, StateFields.Modified(world, character, "thwart", facts, world.Players));

        scheme.PlaceTokens("k_threat", -removed);
        events.Add(new FieldSet(scheme.ObjectId, "k_threat", held, held - removed)
        {
            Trigger = ThwartVerb, Verb = ThwartVerb,
        });
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
