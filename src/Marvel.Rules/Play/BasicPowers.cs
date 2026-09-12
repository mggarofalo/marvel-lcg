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

    /// <inheritdoc cref="BasicPowerInitiation.CardAttack"/>
    public static bool CardAttack(
        World world, ICardFacts facts, int player, Card source, Card enemy, long amount,
        string trigger, List<GameEvent> events, bool overkill = false, Card? moveFrom = null,
        int abilityIndex = -1, int powerOrdinal = 0, int resumeFrom = -1,
        bool finalStep = false, IReadOnlyList<int>? targets = null, bool nested = false,
        bool surgeGained = false, IReadOnlyList<string>? abilityPath = null,
        string abilityFace = "", IReadOnlyDictionary<string, long>? abilityResults = null,
        Occurrence? abilityOccurrence = null, IReadOnlyList<int>? discarded = null,
        bool eachPlayerFrame = false, bool finalPlayer = false, int abilityPlayer = -1,
        bool abilityHasContinuation = false, Card? performer = null) =>
        BasicPowerInitiation.CardAttack(
            world, facts, player, source, enemy, amount, trigger, events, overkill,
            moveFrom, abilityIndex, powerOrdinal, resumeFrom, finalStep, targets,
            nested, surgeGained, abilityPath, abilityFace, abilityResults,
            abilityOccurrence, discarded, eachPlayerFrame, finalPlayer,
            abilityPlayer, abilityHasContinuation, performer);

    /// <inheritdoc cref="BasicPowerResolution.ResolveCharacterThwart"/>
    public static void ResolveCharacterThwart(
        World world, ICardFacts facts, List<GameEvent> events,
        CharacterThwart? scheduled = null) =>
        BasicPowerResolution.ResolveCharacterThwart(world, facts, events, scheduled);

    /// <inheritdoc cref="BasicPowerResolution.ResolveCharacterAttack"/>
    public static void ResolveCharacterAttack(
        World world, ICardFacts facts, List<GameEvent> events,
        CharacterAttack? scheduled = null) =>
        BasicPowerResolution.ResolveCharacterAttack(world, facts, events, scheduled);

    /// <inheritdoc cref="BasicPowerResolution.Consequential"/>
    public static void Consequential(
        World world, ICardFacts facts, Card ally, bool byAttack, string verb,
        List<GameEvent> events) =>
        BasicPowerResolution.Consequential(world, facts, ally, byAttack, verb, events);

    /// <inheritdoc cref="BasicThwartPowers.BasicThwart"/>
    public static void BasicThwart(
        World world, ICardFacts facts, int player, Card scheme, List<GameEvent> events) =>
        BasicThwartPowers.BasicThwart(world, facts, player, scheme, events);

    /// <inheritdoc cref="BasicThwartPowers.CardThwart"/>
    public static bool CardThwart(
        World world, ICardFacts facts, int player, Card source, Card scheme, long amount,
        string trigger, List<GameEvent> events, int abilityIndex = -1,
        int powerOrdinal = 0, int resumeFrom = -1, bool finalStep = false,
        IReadOnlyList<int>? targets = null, ThreatPlacement? imminentThreat = null,
        bool automaticTarget = false, bool nested = false, bool surgeGained = false,
        IReadOnlyList<string>? abilityPath = null, string abilityFace = "",
        IReadOnlyDictionary<string, long>? abilityResults = null,
        Occurrence? abilityOccurrence = null, IReadOnlyList<int>? discarded = null,
        bool eachPlayerFrame = false, bool finalPlayer = false, int abilityPlayer = -1,
        bool abilityHasContinuation = false, Card? performer = null) =>
        BasicThwartPowers.CardThwart(
            world, facts, player, source, scheme, amount, trigger, events,
            abilityIndex, powerOrdinal, resumeFrom, finalStep, targets,
            imminentThreat, automaticTarget, nested, surgeGained, abilityPath,
            abilityFace, abilityResults, abilityOccurrence, discarded,
            eachPlayerFrame, finalPlayer, abilityPlayer, abilityHasContinuation,
            performer);

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

    /// <summary>Initiates an attack using an opaque Cards-owned continuation payload.</summary>
    public static bool CardAttack(
        World world, ICardFacts facts, int player, Card source, Card enemy, long amount,
        string trigger, List<GameEvent> events, CardPowerContinuation continuation,
        IReadOnlyList<int> targets, Card? performer = null) => BasicPowerInitiation.CardAttack(
            world, facts, player, source, enemy, amount, trigger, events,
            abilityIndex: continuation.AbilityIndex, powerOrdinal: continuation.PowerOrdinal,
            resumeFrom: continuation.ResumeFrom, finalStep: continuation.FinalStep,
            targets: targets, nested: true, surgeGained: continuation.SurgeGained,
            abilityPath: continuation.AbilityPath, abilityFace: continuation.AbilityFace,
            abilityResults: continuation.AbilityResults, abilityOccurrence: continuation.AbilityOccurrence,
            discarded: continuation.Discarded, eachPlayerFrame: continuation.EachPlayerFrame,
            finalPlayer: continuation.FinalPlayer, abilityPlayer: continuation.AbilityPlayer,
            abilityHasContinuation: continuation.AbilityHasContinuation, performer: performer);

    /// <summary>Initiates a thwart using an opaque Cards-owned continuation payload.</summary>
    public static bool CardThwart(
        World world, ICardFacts facts, int player, Card source, Card scheme, long amount,
        string trigger, List<GameEvent> events, CardPowerContinuation continuation,
        IReadOnlyList<int> targets, ThreatPlacement? imminentThreat, bool automaticTarget,
        Card? performer = null) => BasicThwartPowers.CardThwart(
            world, facts, player, source, scheme, amount, trigger, events,
            abilityIndex: continuation.AbilityIndex, powerOrdinal: continuation.PowerOrdinal,
            resumeFrom: continuation.ResumeFrom, finalStep: continuation.FinalStep,
            targets: targets, imminentThreat: imminentThreat, automaticTarget: automaticTarget,
            nested: true, surgeGained: continuation.SurgeGained,
            abilityPath: continuation.AbilityPath, abilityFace: continuation.AbilityFace,
            abilityResults: continuation.AbilityResults, abilityOccurrence: continuation.AbilityOccurrence,
            discarded: continuation.Discarded, eachPlayerFrame: continuation.EachPlayerFrame,
            finalPlayer: continuation.FinalPlayer, abilityPlayer: continuation.AbilityPlayer,
            abilityHasContinuation: continuation.AbilityHasContinuation, performer: performer);
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

        var enemies = BasicPowerStatus.Enemies(world, facts);
        bool guarded = enemies.Any(enemy =>
            FacedownDrones.Kind(enemy, facts) == CardKind.Minion
            && BasicPowerStatus.Engaged(world, enemy) == player
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

        // `rr:patrol.1`, as a constant ability: "the engaged player cannot
        // thwart the main scheme." The main scheme only -- a side scheme is
        // still fair game, which is the difference from `rr:guard`.
        bool patrolled = BasicPowerStatus.Patrolled(world, facts, player);

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
                && world.ThreatAbilities.CanRemoveThreat(world, scheme)));
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
