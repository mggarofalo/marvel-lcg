using static Marvel.Rules.Play.AttackBoost;
using static Marvel.Rules.Play.AttackDamage;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// An enemy attack, as <c>rr:attack-enemy-activation</c> lists its steps.
/// </summary>
/// <remarks>
/// <para>
/// The six steps are six entries on the agenda rather than six calls, for the
/// reason the whole agenda exists: step 2 asks a player whether they want to
/// defend, and a phase that is a call has nowhere to stop. See
/// <see cref="Agenda"/>.
/// </para>
/// <para>
/// <b>Where the interrupts go.</b> Not on any of the six. An interrupt that
/// triggers "when [enemy] attacks" is timed to the attack <i>initiating</i> —
/// <c>rr:attack-enemy-activation.5</c> says so in as many words, that such
/// interrupts "have the same timing as interrupts that trigger 'when [the
/// villain/an enemy] initiates an attack'". So the window that matters is the
/// one around <see cref="Steps.Attack"/> itself, before the boost card is even
/// given, and that is where Charge grants overkill and Spider-Sense draws a
/// card.
/// </para>
/// </remarks>
public static class Attack
{
    // The rulebook does not define an engine key for the defender retained by
    // a defense-labeled ability. This persisted lasting-effect spelling is ours.
    internal const string DefenseFallback = "defenseFallback";

    // Likewise, this is the engine's persisted marker that step 5 has applied.
    internal const string AttackDamageResolved = "attackDamageResolved";

    /// <summary>
    /// The affordance verb for the basic defense power —
    /// <c>rr:defend-defense.2</c>.
    /// </summary>
    /// <remarks>
    /// Spelled <c>Defense</c>, with the other three basic powers — see
    /// <see cref="BasicPowers"/>, which holds them and the reasoning. These
    /// strings are on the wire, so a verb invented here would be a divergence
    /// a client would render.
    /// </remarks>
    public const string DefenseVerb = "Defense";

    /// <summary>Expose the imminent attack to initiation interrupts.</summary>
    /// <remarks>
    /// Card instructions can declare a defender "when an enemy attacks". By
    /// <c>rr:attack-enemy-activation.5</c>, those interrupts share the attack
    /// initiation window, before the occurrence itself applies. The pending
    /// attack therefore has to be saveable board state while that window is
    /// open; its six steps are still scheduled only when the occurrence
    /// applies.
    /// </remarks>
    public static void Prepare(World world, ICardFacts facts, PhaseStep step)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        if (world.Attack is not null)
        {
            return;
        }

        var target = step.Character >= 0
            ? world.Cards[step.Character]
            : world.Seats[step.Seat].IdentityCard;
        world.Attack = new EnemyAttack(step.Subject, step.Seat, target.ObjectId);
        world.Activation = new EnemyActivation(
            step.Subject, step.Seat, Attacking: true, Id: step.ActivationId);
        world.FinishedAttack = null;
    }

    /// <summary>Discard an imminent attack replaced by a higher-priority status card.</summary>
    public static void CancelPrepared(World world, int enemy)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.Attack is { Enemy: var attacker } && attacker == enemy)
        {
            world.Attack = null;
        }
        if (world.Activation is { Enemy: var activating, Attacking: true }
            && activating == enemy)
        {
            world.Activation = null;
        }
    }

    /// <summary>
    /// During an attack's initiation interrupt, make that same attack resolve
    /// against every other hero in deterministic seat order.
    /// </summary>
    public static void AlsoResolveAgainstEachOtherHero(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var step = world.Agenda.Current;
        if (step is not { What: Steps.Attack } || world.Agenda.Stage != Stage.Interrupts)
        {
            throw new RulesNotImplementedException(
                "additional attack targets were requested outside an attack initiation");
        }

        world.PendingAdditionalAttackPlayers = Enumerable.Range(0, world.Players)
            .Where(player => player != step.Value.Seat)
            .Where(player => !world.Seats[player].Eliminated)
            .Where(player => world.Facts.Kind(world.Seats[player].IdentityCard.FaceId)
                == CardKind.Hero)
            .ToList();
    }

    /// <summary>Whether a player may begin resolving a defense-labeled ability.</summary>
    /// <remarks>
    /// Outside an attack the label changes no roles —
    /// <c>rr:defend-defense.4.8</c> — so the ability remains legal. During an
    /// attack, <c>.4.6</c> locks defense abilities to the player already
    /// defending, whether the defender is that player's identity or ally.
    /// </remarks>
    public static bool CanUseDefenseAbility(World world, int player) =>
        AttackAbilityDefense.CanUseDefenseAbility(world, player);

    /// <summary>Establish the roles created by a defense-labeled ability.</summary>
    /// <remarks>
    /// This runs before the ability's effect. It neither exhausts the identity
    /// nor marks a basic defense, so DEF is not applied —
    /// <c>rr:defend-defense.4.1</c>, <c>.4.3</c>, and <c>.4.4</c>.
    /// </remarks>
    public static void BeginDefenseAbility(World world, int player)
        => BeginDefenseAbility(world, player, world.Seats[player].IdentityCard);

    /// <summary>Establish the roles for a defense performed by an attributed card.</summary>
    public static void BeginDefenseAbility(World world, int player, Card performer) =>
        AttackAbilityDefense.BeginDefenseAbility(world, player, performer);

    /// <summary>Whether a card instruction can declare this character the defender.</summary>
    /// <remarks>
    /// Card-declared defenders do not use the ordinary step-2 readiness check:
    /// <c>rr:defend-defense.2.2</c> and <c>.3.3</c> expressly permit an ability
    /// that declares without exhausting to name an exhausted hero or ally.
    /// A defense-labeled ability may already have established the same
    /// character as a non-basic defender before its printed effect reaches the
    /// declaration, so naming that same character remains legal.
    /// </remarks>
    public static bool CanDeclareByAbility(
        World world, ICardFacts facts, Card defender, int replaceableDefender = -1) =>
        AttackAbilityDefense.CanDeclareByAbility(world, facts, defender, replaceableDefender);

    /// <summary>Apply a card instruction that declares a hero or ally the defender.</summary>
    /// <remarks>
    /// <c>rr:defend-defense.2.1</c> makes a card-declared hero a basic
    /// defender, including its DEF reduction. <c>.3.2</c> makes a
    /// card-declared ally the defender without a DEF reduction. Exhaustion is
    /// deliberately not performed here: it is a separate printed instruction,
    /// and <c>.2.2</c>/<c>.3.3</c> allow declarations that explicitly happen
    /// without exhausting even when the character is already exhausted.
    /// </remarks>
    public static void DeclareByAbility(
        World world, ICardFacts facts, Card defender, int replaceableDefender = -1) =>
        AttackAbilityDefense.DeclareByAbility(world, facts, defender, replaceableDefender);

    /// <summary>
    /// The attack initiates: it targets a player, and its steps go on the
    /// agenda.
    /// </summary>
    /// <remarks>
    /// <c>rr:attack-enemy-activation</c>: "when an enemy initiates an attack,
    /// it targets a specific player, then resolves that attack against that
    /// player". Targeting is part of initiating and is settled before the
    /// steps, which is why it is here and not a step of its own.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="step">The attack step, whose subject is the attacker.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Initiate(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) =>
        AttackInitiation.Initiate(world, facts, step, events);

    /// <summary>Move a multi-hero attack to its next printed hero target.</summary>
    public static void NextTarget(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        var attack = Current(world);
        if (!attack.RemainingPlayers.Contains(player))
        {
            throw new RulesNotImplementedException(
                $"player {player} is not a remaining target of this attack");
        }

        world.Attack = attack with
        {
            Player = player,
            Target = world.Seats[player].IdentityCard.ObjectId,
            Defender = -1,
            BasicDefense = false,
            CalculatedDamage = null,
            DefensePlayersPassed = [],
            AdditionalPlayers = attack.RemainingPlayers.Where(seat => seat != player).ToList(),
        };
    }

    /// <summary>
    /// Step 1. One facedown boost card from the encounter deck —
    /// <c>rr:attack-enemy-activation.step.1</c>.
    /// </summary>
    /// <remarks>
    /// It waits facedown <i>on the enemy</i> until step 3, which is not
    /// fastidiousness: <c>rr:boost-boost-icon</c> puts the flip "after any
    /// defenders are declared if the villain is attacking", so a defender is
    /// chosen without knowing what the boost card is.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void GiveBoostCard(World world, ICardFacts facts, List<GameEvent> events) => AttackBoost.GiveBoostCard(world, facts, events);
    /// <inheritdoc cref="AttackBoost.GiveAdditionalBoostCard"/>
    public static void GiveAdditionalBoostCard(World world, Card enemy, string trigger, List<GameEvent> events) => AttackBoost.GiveAdditionalBoostCard(world, enemy, trigger, events);
    /// <inheritdoc cref="AttackDefense.DeclareDefender"/>
    public static Prompt? DeclareDefender(World world, ICardFacts facts, IAttackCardAbilities abilities) => AttackDefense.DeclareDefender(world, facts, abilities);
    /// <inheritdoc cref="AttackDefense.Defend"/>
    public static void Defend(World world, ICardFacts facts, IAttackCardAbilities abilities, Decision input, List<GameEvent> events) => AttackDefense.Defend(world, facts, abilities, input, events);
    /// <inheritdoc cref="AttackBoost.FlipBoostCards"/>
    public static void FlipBoostCards(World world, ICardFacts facts, IAttackCardAbilities abilities, List<GameEvent> events) => AttackBoost.FlipBoostCards(world, facts, abilities, events);
    /// <inheritdoc cref="AttackBoost.FinishBoostCard"/>
    public static void FinishBoostCard(World world, ICardFacts facts, IAttackCardAbilities abilities, PhaseStep step, List<GameEvent> events) => AttackBoost.FinishBoostCard(world, facts, abilities, step, events);

    /// <inheritdoc cref="AttackDamage.CalculateDamage"/>
    public static void CalculateDamage(World world, ICardFacts facts) => AttackDamage.CalculateDamage(world, facts);
    /// <inheritdoc cref="AttackDamage.MakeIndirect"/>
    public static void MakeIndirect(World world) => AttackDamage.MakeIndirect(world);
    /// <inheritdoc cref="AttackDamage.DealDamage"/>
    public static void DealDamage(World world, ICardFacts facts, List<GameEvent> events) => AttackDamage.DealDamage(world, facts, events);
    /// <inheritdoc cref="AttackDamage.IndirectDamagePrompt"/>
    public static Prompt IndirectDamagePrompt(World world, ICardFacts facts, PhaseStep step) => AttackDamage.IndirectDamagePrompt(world, facts, step);
    /// <inheritdoc cref="AttackDamage.AssignIndirectDamage"/>
    public static void AssignIndirectDamage(World world, ICardFacts facts, PhaseStep step, Decision input, List<GameEvent> events) => AttackDamage.AssignIndirectDamage(world, facts, step, input, events);
    /// <inheritdoc cref="AttackDamage.PrepareIndirectDamage"/>
    public static long PrepareIndirectDamage(World world, PhaseStep step, List<GameEvent> events) => AttackDamage.PrepareIndirectDamage(world, step, events);
    /// <inheritdoc cref="AttackDamage.ApplyIndirectDamage"/>
    public static void ApplyIndirectDamage(World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) => AttackDamage.ApplyIndirectDamage(world, facts, step, events);
    /// <inheritdoc cref="AttackDamage.FinishIndirectDamage"/>
    public static void FinishIndirectDamage(World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) => AttackDamage.FinishIndirectDamage(world, facts, step, events);

    /// <inheritdoc cref="AttackCompletion.Amount"/>
    public static long Amount(World world, ICardFacts facts, EnemyAttack attack) => AttackCompletion.Amount(world, facts, attack);
    /// <inheritdoc cref="AttackCompletion.End"/>
    public static void End(World world, List<GameEvent> events) => AttackCompletion.End(world, events);
    /// <inheritdoc cref="AttackCompletion.EndForEliminatedPlayer"/>
    public static void EndForEliminatedPlayer(World world, List<GameEvent> events) => AttackCompletion.EndForEliminatedPlayer(world, events);
    /// <inheritdoc cref="AttackCompletion.Over"/>
    public static bool Over(World world) => AttackCompletion.Over(world);
    /// <inheritdoc cref="AttackCompletion.RefreshDefender"/>
    public static void RefreshDefender(World world, ICardFacts facts) => AttackCompletion.RefreshDefender(world, facts);
}
