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
    /// <inheritdoc cref="BasicPowerInitiation.BasicAttack"/>
    public static void BasicAttack(
        World world, ICardFacts facts, int player, Card enemy, List<GameEvent> events) =>
        BasicPowerInitiation.BasicAttack(world, facts, player, enemy, events);

    /// <inheritdoc cref="BasicPowerInitiation.BasicAttackWithoutExhausting"/>
    public static void BasicAttackWithoutExhausting(
        World world, ICardFacts facts, Card character, Card enemy,
        List<GameEvent> events) =>
        BasicPowerInitiation.BasicAttackWithoutExhausting(
            world, facts, character, enemy, events);





    /// <inheritdoc cref="BasicPowerResolution.Consequential"/>
    public static void Consequential(
        World world, ICardFacts facts, Card ally, bool byAttack, string verb,
        List<GameEvent> events) =>
        BasicPowerResolution.Consequential(world, facts, ally, byAttack, verb, events);

    /// <inheritdoc cref="BasicThwartPowers.BasicThwart"/>
    public static void BasicThwart(
        World world, ICardFacts facts, int player, Card scheme, List<GameEvent> events) =>
        BasicThwartPowers.BasicThwart(world, facts, player, scheme, events);

    /// <inheritdoc cref="BasicThwartPowers.CanAutomaticallyThwart"/>
    public static bool CanAutomaticallyThwart(
        World world, ICardFacts facts, int player, Card scheme) =>
        BasicThwartPowers.CanAutomaticallyThwart(world, facts, player, scheme);

    /// <inheritdoc cref="BasicThwartPowers.UsesAttack"/>
    public static bool UsesAttack(World world, ICardFacts facts, Card scheme) =>
        BasicThwartPowers.UsesAttack(world, facts, scheme);

    /// <inheritdoc cref="AllyBasicPowers.BasicRecovery"/>
    public static void BasicRecovery(
        World world, ICardFacts facts, int player, List<GameEvent> events) =>
        AllyBasicPowers.BasicRecovery(world, facts, player, events);

    /// <inheritdoc cref="AllyBasicPowers.Allies"/>
    public static IReadOnlyList<Card> Allies(World world, int player) =>
        AllyBasicPowers.Allies(world, player);

    /// <inheritdoc cref="AllyBasicPowers.AllyPower"/>
    public static void AllyPower(
        World world, ICardFacts facts, Card ally, Card target, string verb,
        List<GameEvent> events) =>
        AllyBasicPowers.AllyPower(world, facts, ally, target, verb, events);

    /// <inheritdoc cref="BasicPowerStatus.Cancelled"/>
    public static bool Cancelled(
        World world, ICardFacts facts, Card character, string status,
        List<GameEvent> events) =>
        BasicPowerStatus.Cancelled(world, facts, character, status, events);

    /// <summary>Whether a character has a usable printed value for one basic power.</summary>
    public static bool CanUsePower(ICardFacts facts, Card character, string field)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentException.ThrowIfNullOrEmpty(field);

        return StateFieldCatalog.HasUsablePrintedPower(facts, character.FaceId, field);
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

        // pack:mc11:game-areas: "Players cannot attack or defend enemies in other
        // game areas." An ordinary game has one game area, so nothing is removed.
        var identity = world.Seats[player].IdentityCard;
        var enemies = BasicPowerStatus.Enemies(world, facts)
            .Where(enemy => Places.CanAffect(world, identity, enemy)).ToList();
        bool guarded = enemies.Any(enemy =>
            EffectiveCards.Kind(enemy, facts) == CardKind.Minion
            && BasicPowerStatus.Engaged(world, enemy) == player
            && StateFields.Modified(world, enemy, "guard", facts, world.Players) > 0);

        var restricted = enemies.Where(enemy => CardKinds.IsVillain(EffectiveCards.Kind(enemy, facts))
            && StateFields.Modified(world, enemy, VillainAttackRestrictions.OtherVillains,
                facts, world.Players) > 0).ToList();
        return [.. enemies.Where(enemy =>
            EffectiveCards.Kind(enemy, facts) == CardKind.Minion
            || !guarded && VillainAttackRestrictions.Allows(enemy, restricted))];
    }

    /// <summary>The enemies one attacking character may attack right now.</summary>
    /// <remarks>
    /// <see cref="Attackable(World, ICardFacts, int)"/> narrowed by the
    /// prohibitions that bind a single character rather than its player
    /// (<see cref="AttackPermissions"/>).
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="player">Whose character is attacking.</param>
    /// <param name="attacker">The character performing the attack.</param>
    public static IReadOnlyList<Card> Attackable(World world, ICardFacts facts, int player, Card attacker) =>
        [.. Attackable(world, facts, player).Where(enemy => AttackPermissions.CanAttack(world, attacker, enemy))];

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

        // `rr:patrol.1`, as a constant ability: "the engaged player cannot
        // thwart the main scheme." The main scheme only -- a side scheme is
        // still fair game, which is the difference from `rr:guard`.
        bool patrolled = BasicPowerStatus.Patrolled(world, facts, player);

        // `rr:crisis-icon.1`, as a constant ability: "player cards cannot remove
        // threat from the main scheme." A hero's identity and an ally are both
        // player cards, so a crisis icon that can affect the main scheme takes it
        // off everybody's list -- unlike `rr:patrol`, which is one player's.
        // `.2` exempts encounter card abilities, which do not come through here.
        // pack:mc11:game-areas: a player "cannot target any game elements in the
        // other game areas", and an icon there cannot affect this player's scheme.
        var identity = world.Seats[player].IdentityCard;

        var schemes = new List<Card>();
        foreach (var area in world.Areas)
        {
            if (area.Type is not (DeckType.MainSchemesArea or DeckType.SideSchemesArea))
            {
                continue;
            }

            if (area.Type == DeckType.MainSchemesArea && patrolled)
            {
                continue;
            }

            schemes.AddRange(area.Cards.Where(scheme =>
                Places.CanAffect(world, identity, scheme)
                && !(area.Type == DeckType.MainSchemesArea && MainScheme.Crisis(world, facts, scheme))
                && scheme.Tokens.GetValueOrDefault("k_threat") > 0
                && world.ThreatAbilities.CanThwartScheme(world, scheme)));
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
}
