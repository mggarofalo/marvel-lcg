using static Marvel.Rules.Play.AttackBoost;
using static Marvel.Rules.Play.AttackDamage;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// The legal defenders for one attack and whether a defender is mandatory.
/// </summary>
/// <param name="Candidates">The characters that may defend.</param>
/// <param name="Required">Whether declining to defend is illegal.</param>
public sealed record DefenderChoice(IReadOnlyList<Card> Candidates, bool Required);

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
    public static bool CanUseDefenseAbility(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.Attack is not { Defender: >= 0 } attack)
        {
            return true;
        }

        return world.Cards[attack.Defender].Area.PlayArea == PlayArea.Of(player);
    }

    /// <summary>Establish the roles created by a defense-labeled ability.</summary>
    /// <remarks>
    /// This runs before the ability's effect. It neither exhausts the identity
    /// nor marks a basic defense, so DEF is not applied —
    /// <c>rr:defend-defense.4.1</c>, <c>.4.3</c>, and <c>.4.4</c>.
    /// </remarks>
    public static void BeginDefenseAbility(World world, int player)
        => BeginDefenseAbility(world, player, world.Seats[player].IdentityCard);

    /// <summary>Establish the roles for a defense performed by an attributed card.</summary>
    public static void BeginDefenseAbility(World world, int player, Card performer)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(performer);
        if (world.Attack is not { } attack)
        {
            // `rr:defend-defense.4.8`: the label may resolve outside an attack
            // without making an identity the defender of anything.
            return;
        }

        if (!CanUseDefenseAbility(world, player))
        {
            throw new RulesNotImplementedException(
                $"player {player} cannot defend an attack already defended by another player");
        }

        if (attack.Defender >= 0)
        {
            // `rr:defend-defense.4.7`: a defense ability remains legal while
            // this player's ally defends, but the identity does not replace it.
            return;
        }

        bool character = FacedownDrones.Kind(performer, world.Facts) is
            CardKind.Hero or CardKind.AlterEgo or CardKind.Ally;
        if (!character)
        {
            // `rr:support.3` excludes support defenses from the identity. A
            // support is not a character, so it performs the labeled effect
            // without becoming the defending character of the attack.
            return;
        }

        world.Attack = attack with
        {
            Defender = performer.ObjectId,
            Target = performer.ObjectId,
            Player = player,
            BasicDefense = false,
        };
        if (world.Activation is { Attacking: true } activation)
        {
            world.Activation = activation with { Player = player };
        }
    }

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
        World world, ICardFacts facts, Card defender, int replaceableDefender = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(defender);

        if (world.Attack is not { } attack
            || !DeckTypes.IsInPlay(defender.Area.Type)
            || defender.Area.PlayArea.Player < 0)
        {
            return false;
        }

        var kind = FacedownDrones.Kind(defender, facts);
        return kind is CardKind.Hero or CardKind.Ally
            && (attack.Defender < 0
                || attack.Defender == defender.ObjectId
                || attack.Defender == replaceableDefender);
    }

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
        World world, ICardFacts facts, Card defender, int replaceableDefender = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(defender);

        if (!CanDeclareByAbility(world, facts, defender, replaceableDefender))
        {
            throw new RulesNotImplementedException(
                $"card {defender.ObjectId} cannot be declared the defender of the current attack");
        }

        var attack = Current(world);
        if (attack.Defender >= 0
            && attack.Defender == replaceableDefender
            && attack.Defender != defender.ObjectId)
        {
            // Mutant Protectors is the first printed shape: its defense label
            // makes the identity the defender, then its text declares an ally.
            // The official ruling retains that identity as the non-basic
            // defender if the ally leaves before damage. A bounded effect keeps
            // that provenance saveable without adding a second defender role.
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: DefenseFallback,
                Amount: attack.Defender,
                Affects: defender.ObjectId,
                Lasts: Duration.UntilEndOf(TimingPoints.EndOfAttack)));
        }
        int player = defender.Area.PlayArea.Player;
        world.Attack = attack with
        {
            Defender = defender.ObjectId,
            Target = defender.ObjectId,
            Player = player,
            BasicDefense = FacedownDrones.Kind(defender, facts) == CardKind.Hero,
        };
        if (world.Activation is { Attacking: true } activation)
        {
            world.Activation = activation with { Player = player };
        }
    }

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
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:stun-stunned.1`: "**Forced Interrupt**: when this character would
        // attack, remove each stunned status card from it instead." *Instead*
        // -- so the attack does not happen at all, and none of its six steps is
        // scheduled. No boost card is given and no defender is asked for.
        if (BasicPowerStatus.Cancelled(
            world, facts, world.Cards[step.Subject], Statuses.Stunned, events))
        {
            CancelPrepared(world, step.Subject);
            world.PendingAdditionalAttackPlayers = [];
            return;
        }

        // `rr:attack-enemy-activation.1` -- against both a player and a
        // character, and `.1.1` -- "normally the attacked character
        // is the player's hero, but abilities can instead cause an enemy to
        // attack a player's alter-ego or an ally that player controls", and
        // `rr:attacks-against-allies.1` keeps the player attacked either way,
        // so the seat is unchanged and only the character moves.
        Prepare(world, facts, step);
        var additional = world.PendingAdditionalAttackPlayers;
        world.PendingAdditionalAttackPlayers = [];
        world.Attack = Current(world) with { AdditionalPlayers = additional };

        // `rr:activation` -- "whenever an enemy attacks or schemes, it is
        // considered to have activated". The umbrella, which a scheme sets too;
        // `world.Attack` is the six steps below it.
        world.Activation ??= new EnemyActivation(
            step.Subject, step.Seat, Attacking: true, Id: step.ActivationId);

        world.Agenda.Then(new PhaseStep(
            Steps.GiveBoostCard, step.Round, 1, Index: step.Seat, Subject: step.Subject,
            ActivationId: step.ActivationId));
        world.Agenda.Then(new PhaseStep(
            Steps.DeclareDefender, step.Round, 2, Index: step.Seat, Subject: step.Subject,
            Seat: step.Seat, ActivationId: step.ActivationId));
        world.Agenda.Then(new PhaseStep(
            Steps.FlipBoostCards, step.Round, 3, Index: step.Seat, Subject: step.Subject,
            ActivationId: step.ActivationId));
        world.Agenda.Then(new PhaseStep(
            Steps.CalculateAttackDamage, step.Round, 4, Index: step.Seat, Subject: step.Subject,
            Seat: step.Seat, ActivationId: step.ActivationId));
        world.Agenda.Then(new PhaseStep(
            Steps.DealAttackDamage, step.Round, 5, Index: step.Seat, Subject: step.Subject,
            Seat: step.Seat, ActivationId: step.ActivationId));
        foreach (int player in additional)
        {
            world.Agenda.Then(new PhaseStep(
                Steps.NextAttackTarget, step.Round, 5, Index: player, Subject: step.Subject,
                Seat: player, Plan: true, ActivationId: step.ActivationId));
            world.Agenda.Then(new PhaseStep(
                Steps.DeclareDefender, step.Round, 2, Index: player, Subject: step.Subject,
                Seat: player, ActivationId: step.ActivationId));
            world.Agenda.Then(new PhaseStep(
                Steps.CalculateAttackDamage, step.Round, 4, Index: player, Subject: step.Subject,
                Seat: player, ActivationId: step.ActivationId));
            world.Agenda.Then(new PhaseStep(
                Steps.DealAttackDamage, step.Round, 5, Index: player, Subject: step.Subject,
                Seat: player, ActivationId: step.ActivationId));
        }
        world.Agenda.Then(new PhaseStep(
            Steps.EndAttack, step.Round, 6, Index: step.Seat, Subject: step.Subject,
            Seat: step.Seat, ActivationId: step.ActivationId));
    }

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
    /// <inheritdoc cref="AttackBoost.DeclareDefender"/>
    public static Prompt? DeclareDefender(World world, ICardFacts facts, IAttackCardAbilities abilities) => AttackBoost.DeclareDefender(world, facts, abilities);
    /// <inheritdoc cref="AttackBoost.Defend"/>
    public static void Defend(World world, ICardFacts facts, IAttackCardAbilities abilities, Decision input, List<GameEvent> events) => AttackBoost.Defend(world, facts, abilities, input, events);
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
