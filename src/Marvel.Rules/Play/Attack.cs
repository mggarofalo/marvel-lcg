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
    private const string DefenseFallback = "defenseFallback";

    // Likewise, this is the engine's persisted marker that step 5 has applied.
    private const string AttackDamageResolved = "attackDamageResolved";

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
        if (BasicPowers.Cancelled(
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
    public static void GiveBoostCard(World world, ICardFacts facts, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:activation.6` -- "if an activating minion leaves play, that
        // minion's activation ends immediately and no further steps of that
        // activation resolve."
        if (Over(world))
        {
            return;
        }

        var activation = Activating(world);
        var enemy = world.Cards[activation.Enemy];

        // `rr:attack-enemy-activation.step.1` and
        // `rr:scheme-enemy-activation.step.1`, the same clause twice: only a
        // villain, or a minion with `rr:villainous`, is given one. "If a minion
        // without the villainous keyword is attacking, **skip this step**" --
        // and skipping matters beyond the icons, because taking a card off the
        // encounter deck moves every later deal.
        if (!Keywords.IsBoosted(world, enemy, facts, world.Players))
        {
            return;
        }

        DealBoostCard(world, enemy, Activated(activation), events);
    }

    /// <summary>Give an enemy one additional boost card for its activation.</summary>
    /// <remarks>
    /// <c>rr:boost-boost-icon.4</c> makes additional cards cumulative, and
    /// <c>.6</c> leaves a card given before the activation facedown on the
    /// enemy until it activates. The primitive is immediate so a Boost ability
    /// reached while <see cref="FlipBoostCards"/> is walking the hosted queue
    /// adds the next card that same loop resolves.
    /// </remarks>
    public static void GiveAdditionalBoostCard(
        World world, Card enemy, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);
        ArgumentNullException.ThrowIfNull(events);

        DealBoostCard(world, enemy, trigger, events);
    }

    private static void DealBoostCard(
        World world, Card enemy, string trigger, List<GameEvent> events)
    {
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var boost = EncounterDeck.TakeTop(world, trigger, events);
        if (boost is null)
        {
            return;
        }

        var onto = BoostCards(world, enemy.ObjectId);
        onto.Append(boost);
        events.Add(new CardsMoved(
            Places.Reference(deck), Places.Reference(onto),
            [new Landing(boost.ObjectId, onto.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Boost",
        });
    }

    /// <summary>
    /// Step 2. Whether anybody defends —
    /// <c>rr:attack-enemy-activation.step.2</c>.
    /// </summary>
    /// <remarks>
    /// Asked only where there is something to ask. A player with no ready
    /// character cannot defend (<c>rr:defend-defense.2</c> and <c>.3</c> both
    /// require exhausting one), so there is no question to put and the step
    /// passes in silence — the same rule <see cref="Offering"/> applies to
    /// windows.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">Card-specific defender restrictions.</param>
    /// <returns>The question, or null when nobody could defend.</returns>
    public static Prompt? DeclareDefender(
        World world, ICardFacts facts, IAttackCardAbilities abilities)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);

        // `rr:activation.6` -- "if an activating minion leaves play, that
        // minion's activation ends immediately and no further steps of that
        // activation resolve."
        if (Over(world))
        {
            return null;
        }

        var attack = Current(world);
        var choice = Choice(world, facts, abilities, attack);
        if (choice.Candidates.Count == 0)
        {
            return null;
        }

        var seat = world.Seats[attack.Player];
        Card enemy = world.Cards[attack.Enemy];
        long attackValue = StateFields.Modified(
            world, enemy, "attack", facts, world.Players);
        return new Prompt(
            Player: attack.Player,
            Asking: Question.Defender,
            When: TimingPriority.Untimed,
            Trigger: Steps.AttackInitiated,
            Label: $"{seat.Name} declares a defender",

            // `rr:attack-enemy-activation.4` ordinarily permits no defender.
            // A card instruction can make defending mandatory when its stated
            // kind of defender is able, which is why this is card-provided.
            Cancellable: !choice.Required,
            Affordances:
            [
                // `AnchorPlayer` is whose character it is, which is not
                // always the player being asked: `rr:defend-defense.5` lets
                // somebody else's hero or ally defend, and taking over makes
                // them the attack's new target.
                .. choice.Candidates.Select(card => new Affordance(
                    Id: card.ObjectId,
                    Verb: DefenseVerb,
                    AnchorId: card.ObjectId,
                    AnchorPlayer: card.Area.PlayArea.Player,
                    Label: DefenseVerb)),
            ])
        {
            Description = $"{facts.Title(enemy.FaceId)} is attacking "
                + $"{facts.Title(world.Cards[attack.Target].FaceId)}. "
                + $"ATK {attackValue} before facedown boost cards. "
                + "Choose a ready hero or ally to defend, or leave the attack undefended.",
        };
    }

    /// <summary>The character that answer names becomes the defender.</summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">Card-specific defender restrictions.</param>
    /// <param name="input">The player's answer.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Defend(
        World world, ICardFacts facts, IAttackCardAbilities abilities, Decision input,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(events);

        var attack = Current(world);
        var choice = Choice(world, facts, abilities, attack);
        if (input.IsDecline)
        {
            if (choice.Required)
            {
                throw new RulesNotImplementedException(
                    $"the attack by card {attack.Enemy} requires a defender and cannot be "
                    + "declined");
            }

            return;
        }

        var defender = choice.Candidates
            .FirstOrDefault(card => card.ObjectId == input.Affordance)
            ?? throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered as a defender");

        // rr:defend-defense.2 and .3 -- both require exhausting the defender.
        defender.Exhaust();
        events.Add(new FieldSet(defender.ObjectId, "is_exhaust", 0, 1)
        {
            Trigger = Steps.AttackInitiated, Verb = DefenseVerb,
        });

        // **The defender becomes the target, whoever they are.**
        // `rr:defend-defense.3.1` for an ally: "that ally becomes the target
        // character for that attack, and its controller becomes the target
        // player". `rr:defend-defense.2` for a hero: "any remaining damage is
        // dealt to that hero". And `.5`: "if a player defends against an enemy
        // attack that targets a different player [...] the defending player
        // becomes the new target of that attack."
        //
        // Three clauses, one move. When the target player defends with their
        // own hero it changes nothing, which is why it read as "the target does
        // not change" while one player was all the engine had.
        //
        // `BasicDefense` is the hero's alone: `rr:defend-defense.2`'s reduction
        // belongs to the basic defense power, and `.3` gives an ally none.
        world.Attack = attack with
        {
            Defender = defender.ObjectId,
            Target = defender.ObjectId,
            Player = defender.Area.PlayArea.Player,
            BasicDefense = facts.Kind(defender.FaceId) != CardKind.Ally,
        };
        if (world.Activation is { Attacking: true } activation)
        {
            world.Activation = activation with { Player = defender.Area.PlayArea.Player };
        }
    }

    /// <summary>
    /// Step 3. Flip each boost card, apply its icons, discard it —
    /// <c>rr:attack-enemy-activation.step.3</c>.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do, for a boost card that has one.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void FlipBoostCards(
        World world, ICardFacts facts, IAttackCardAbilities abilities, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:activation.6` -- "if an activating minion leaves play, that
        // minion's activation ends immediately and no further steps of that
        // activation resolve."
        if (Over(world))
        {
            return;
        }

        var activation = Activating(world);
        var waiting = BoostCards(world, activation.Enemy);
        string trigger = Activated(activation);

        if (waiting.Cards.Count == 0)
        {
            return;
        }

        // "One at a time and in the order in which they were dealt", which is
        // bottom-first. This call handles exactly one card so an ability that
        // asks a question can suspend before step 3c or the next card begins.
        var boost = waiting.Cards[0];

        // Through the boosting area: passing through is what registers the
        // card's token pools, and the discarded card's `k_threat` key is on
        // the wire.
        World.MoveToTop(boost, world.AreaOf(DeckType.BoostingArea));
        world.RecordInformation(InformationKind.Reveal);
        events.Add(new CardsFlipped([boost.ObjectId], true)
        {
            Trigger = trigger, Verb = "Boost",
        });

        // rr:attack-enemy-activation.step.3.b and rr:boost-boost-icon.2 --
        // resolve the ability before its icons are applied or it is discarded.
        var occurrence = world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "a boost ability has no activation occurrence");
        int beforeAbility = world.Agenda.Count;
        events.AddRange(abilities.Boost(world, boost, activation.Player));
        if (world.Agenda.Count > beforeAbility)
        {
            // An ordinary authored choice is still scheduled after the flip;
            // move that child ahead first. A nested rules procedure has
            // already done this for itself and made its child current.
            if (world.Agenda.Current is { What: Steps.FlipBoostCards })
            {
                world.Agenda.BeforeResponses(occurrence);
            }
            world.Agenda.BeforeOwnerAfterContinuations(
                occurrence,
                Steps.FlipBoostCards,
                new PhaseStep(
                    Steps.FinishBoostCard,
                    world.Agenda.Current?.Round ?? 0,
                    3,
                    Subject: boost.ObjectId,
                    Seat: activation.Player,
                    Character: activation.Enemy,
                    ProcedureFlag: activation.Attacking));
            return;
        }

        FinishBoostCard(
            world, facts, abilities,
            new PhaseStep(
                Steps.FinishBoostCard, 0, 3,
                Subject: boost.ObjectId,
                Seat: activation.Player,
                Character: activation.Enemy,
                ProcedureFlag: activation.Attacking),
            events);
    }

    /// <summary>Apply step 3c and 3d after one boost ability has completed.</summary>
    public static void FinishBoostCard(
        World world, ICardFacts facts, IAttackCardAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        var boost = world.Cards[step.Subject];
        string trigger = step.ProcedureFlag ? Steps.AttackInitiated : Steps.EnemySchemes;

        // A Boost ability can end the activation. Its assigned boost card is
        // still cleaned up, but rr:activation.6 permits no icon application,
        // later boost card, or other activation step afterwards.
        if (world.Activation is not { } activation
            || activation.Enemy != step.Character
            || activation.Player != step.Seat
            || activation.Attacking != step.ProcedureFlag
            || !DeckTypes.IsInPlay(world.Cards[step.Character].Area.Type))
        {
            DiscardBoostCard(world, boost, trigger, events);
            return;
        }

        // rr:attack-enemy-activation.step.3.c -- count icons after the Boost
        // ability, because step 3b precedes this one. Amplify applies per card.
        long icons = Characteristics.IsLost(world, boost, "boost_const")
            ? 0
            : StateFields.Modified(world, boost, "boost_const", facts, world.Players)
                + MainScheme.Amplify(world, facts);
        if (icons > 0)
        {
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: activation.Attacking ? "attack" : "scheme",
                Amount: icons,
                Card: boost.ObjectId,
                Affects: activation.Enemy,
                Lasts: Duration.UntilEndOf(activation.Attacking
                    ? TimingPoints.EndOfAttack
                    : TimingPoints.EndOfActivation)));
        }

        // A Boost ability can move itself into play. Step 3d discards the card
        // only while it remains the boost card being applied.
        DiscardBoostCard(world, boost, trigger, events);

        // Step 3e reaches the next card only after this card's four preceding
        // substeps have completed.
        FlipBoostCards(world, facts, abilities, events);
    }

    private static void DiscardBoostCard(
        World world, Card boost, string trigger, List<GameEvent> events)
    {
        if (boost.Area.Type != DeckType.BoostingArea)
        {
            return;
        }

        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        var from = boost.Area;
        World.MoveToTop(boost, discard);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(discard),
            [new Landing(boost.ObjectId, discard.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Boost",
        });
    }

    /// <summary>
    /// Step 4. Calculate the attack's damage —
    /// <c>rr:attack-enemy-activation.step.4</c>.
    /// </summary>
    /// <remarks>
    /// The amount is board state because step 5 is a later occurrence. An
    /// effect after this step may change ATK or DEF, but step 5 deals "the
    /// amount of damage calculated in the previous step" rather than asking
    /// the board to calculate it again.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    public static void CalculateDamage(World world, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        if (Over(world))
        {
            return;
        }

        RefreshDefender(world, facts);
        var attack = Current(world);
        world.Attack = attack with { CalculatedDamage = Amount(world, facts, attack) };
    }

    /// <summary>Make the current enemy attack deal indirect damage.</summary>
    public static void MakeIndirect(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.Attack = Current(world) with { Indirect = true };
    }

    /// <summary>
    /// Step 5. Deal the damage calculated in step 4 —
    /// <c>rr:attack-enemy-activation.step.5</c>.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void DealDamage(World world, ICardFacts facts, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:activation.6` -- "if an activating minion leaves play, that
        // minion's activation ends immediately and no further steps of that
        // activation resolve."
        if (Over(world))
        {
            return;
        }

        RefreshDefender(world, facts);
        var attack = Current(world);
        long amount = attack.CalculatedDamage
            ?? throw new RulesNotImplementedException(
                "attack damage reached step 5 before step 4 calculated it");

        // The departure rule applies only before attack damage is dealt. The
        // step is resolved even when its calculated amount is zero, so mark it
        // before that early return. The marker also prevents a nested damage
        // consequence from rewriting the attack after assignment has begun.
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: AttackDamageResolved,
            Affects: attack.Target,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfAttack)));
        if (amount <= 0)
        {
            return;
        }

        if (attack.Indirect)
        {
            var candidates = IndirectCandidates(world, facts, attack.Player);
            long assign = Math.Min(
                amount,
                candidates.Sum(card => Damage.Health(world, facts, card) - card.Damage));
            if (assign <= 0)
            {
                return;
            }

            var assignment = new PhaseStep(
                Steps.AssignIndirectAttackDamage,
                world.Agenda.Current?.Round ?? 0,
                5,
                Subject: attack.Enemy,
                Seat: attack.Player,
                Character: attack.Target,
                ProcedureAmount: assign,
                ProcedureOccurrence: world.Agenda.Occurrence,
                ProcedureCandidates: [.. candidates.Select(card => card.ObjectId)]);
            if (candidates.Count == 1)
            {
                AssignIndirectDamage(
                    world, facts, assignment,
                    Decision.Take(
                        attack.Enemy,
                        Enumerable.Repeat(candidates[0].ObjectId, (int)assign).ToList(),
                        []),
                    events);
            }
            else
            {
                var occurrence = world.Agenda.Occurrence
                    ?? throw new RulesNotImplementedException(
                        "indirect attack damage has no containing occurrence");
                world.Agenda.ThenContinuation(
                    assignment,
                    occurrence);
                world.Agenda.BeforeResponses(occurrence);
            }
            return;
        }

        // Through the same primitive a hero's basic attack uses. `rr:damage` is
        // one rule however the damage arrived, and so is `rr:defeat` -- an
        // enemy attack that defeated a character down a separate path would be
        // a second place for the defeat rules to be wrong.
        // One call, because `rr:piercing`, `rr:overkill` and `rr:ranged` are all
        // properties of the attack rather than of either character.
        var damage = Damage.Attack(
            world, facts, world.Cards[attack.Enemy], world.Cards[attack.Target], amount,
            Steps.AttackInitiated, "Deal_Damage", events);

        // Recorded on the attack rather than derived later, because by the time
        // `rr:attack-enemy-activation.step.6.a`'s abilities run the damage is on
        // a dial that had damage on it before. `damaged` is the list
        // `rr:tough.3` shortens -- a character whose tough card absorbed the
        // attack "is not considered to have taken damage" -- so an attack that
        // hit a tough card did not damage anybody.
        if (damage.Characters.Count > 0)
        {
            if (world.Attack is not null)
            {
                world.Attack = attack with { Damaged = true };
            }
            else if (world.FinishedAttack is { } finishedAttack)
            {
                world.FinishedAttack = finishedAttack with { Damaged = true };
            }

            // Direct rules-unit calls have no surrounding window; an agenda
            // occurrence gains the condition for its shared response window.
            world.Agenda.Occurrence?.Also(Steps.DamageDealt);
        }

        if (world.Activation is { } activation)
        {
            world.Activation = activation with
            {
                DamageDealt = activation.DamageDealt + damage.Dealt,
            };
        }
        else if (world.FinishedActivation is { } finishedActivation)
        {
            world.FinishedActivation = finishedActivation with
            {
                DamageDealt = finishedActivation.DamageDealt + damage.Dealt,
            };
        }
    }

    /// <summary>Ask the attacked player to assign one indirect attack's damage.</summary>
    public static Prompt IndirectDamagePrompt(
        World world, ICardFacts facts, PhaseStep step)
    {
        var candidates = IndirectCandidates(world, facts, step.Seat)
            .Where(card => (step.ProcedureCandidates ?? []).Contains(card.ObjectId))
            .ToList();
        int amount = checked((int)Math.Min(
            step.ProcedureAmount,
            candidates.Sum(card => Damage.Health(world, facts, card) - card.Damage)));
        var maximumOccurrences = candidates.ToDictionary(
            card => card.ObjectId,
            card => checked((int)(Damage.Health(world, facts, card) - card.Damage)));
        return new Prompt(
            step.Seat,
            Question.Element,
            TimingPriority.Untimed,
            Steps.DealAttackDamage,
            $"{world.Seats[step.Seat].Name} assigns {amount} indirect attack damage",
            false,
            [new Affordance(
                step.Subject, "Choose", step.Subject, World.Scenario,
                "indirectDamage",
                new TargetRequest(
                    [.. candidates.Select(card => card.ObjectId)], amount, amount,
                    Rule: "rr:indirect-damage.1",
                    AllowRepeated: true,
                    MaximumOccurrences: maximumOccurrences))]);
    }

    /// <summary>Resolve an assignment without treating every recipient as attacked.</summary>
    public static void AssignIndirectDamage(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        if (input.IsDecline || input.Affordance != step.Subject
            || input.Spent.Count > 0 || input.DefinedValues.Count > 0
            || input.Allocated.Count > 0)
        {
            throw new RulesNotImplementedException(
                "the indirect attack damage answer was not the offered assignment");
        }

        var occurrence = step.ProcedureOccurrence ?? world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "indirect attack damage has no containing occurrence");
        var eligible = IndirectCandidates(world, facts, step.Seat)
            .Where(card => (step.ProcedureCandidates ?? []).Contains(card.ObjectId))
            .ToDictionary(card => card.ObjectId);
        int expected = checked((int)Math.Min(
            step.ProcedureAmount,
            eligible.Values.Sum(card => Damage.Health(world, facts, card) - card.Damage)));
        if (input.Targets.Count != expected)
        {
            throw new RulesNotImplementedException(
                $"indirect attack damage requires {expected} assignments");
        }

        var assigned = new Dictionary<int, long>();
        foreach (int id in input.Targets)
        {
            if (!eligible.TryGetValue(id, out var card))
            {
                throw new RulesNotImplementedException(
                    $"card {id} cannot receive this indirect attack damage");
            }
            long share = assigned.GetValueOrDefault(id) + 1;
            long room = Damage.Health(world, facts, card) - card.Damage;
            if (share > room)
            {
                throw new RulesNotImplementedException(
                    $"card {id} has room for {room} indirect attack damage");
            }
            assigned[id] = share;
        }

        int round = world.Agenda.Current?.Round ?? 0;
        var windows = assigned
            .OrderBy(pair => pair.Key)
            .Select((pair, index) => new PhaseStep(
                Steps.PrepareIndirectAttackDamage,
                round,
                5,
                Index: index,
                Subject: pair.Key,
                Seat: step.Seat,
                ProcedureSource: step.Subject,
                ProcedureAmount: pair.Value,
                ProcedureAmounts: new Dictionary<int, long>(),
                ProcedureOccurrence: occurrence))
            .ToList();
        windows.Add(new PhaseStep(
            Steps.ApplyIndirectAttackDamage,
            round,
            5,
            Subject: step.Subject,
            Seat: step.Seat,
            Character: step.Character,
            Plan: true,
            ProcedureCandidates: [.. input.Targets],
            ProcedureOccurrence: occurrence,
            ProcedureAmounts: new Dictionary<int, long>()));
        world.Agenda.Now(windows);
    }

    /// <summary>Resolve step 1 for one assigned indirect-damage recipient.</summary>
    public static long PrepareIndirectDamage(
        World world, PhaseStep step, List<GameEvent> events)
    {
        if (step.ProcedureOccurrence is not { } procedure)
        {
            throw new RulesNotImplementedException(
                "indirect attack damage has no containing occurrence");
        }
        if (step.ProcedureAmounts?.ContainsKey(step.Subject) == true)
        {
            return step.ProcedureAmounts[step.Subject];
        }

        var attacker = world.Cards[step.ProcedureSource];
        var target = world.Cards[step.Subject];
        long amount = Damage.Replace(
            world, target, attacker, step.ProcedureAmount, events);
        world.Agenda.RecordProcedureAmount(procedure, step.Subject, amount);
        return amount;
    }

    /// <summary>Place an assigned indirect attack after every recipient window.</summary>
    public static void ApplyIndirectDamage(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var occurrence = step.ProcedureOccurrence ?? world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "indirect attack damage has no containing occurrence");
        var assigned = step.ProcedureAmounts
            ?? throw new RulesNotImplementedException(
                "indirect attack damage has no prepared recipient results");

        var attacker = world.Cards[step.Subject];
        var placed = new List<Damage.PlacedDamage>();
        foreach (var (id, amount) in assigned.OrderBy(pair => pair.Key))
        {
            var target = world.Cards[id];
            placed.Add(Damage.PrepareAfterReplacement(
                world, facts, attacker, target, amount,
                Steps.AttackInitiated, events));
        }
        foreach (var damage in placed)
        {
            Damage.ApplyPlaced(
                world, facts, damage, Steps.AttackInitiated, "Attack", events);
        }

        // rr:indirect-damage.3 -- every assigned share is dealt
        // simultaneously. All step-5 placement therefore finishes before a
        // delayed damage effect or defeat can change the board seen by another
        // recipient's already-assigned damage.
        foreach (var damage in placed.Where(damage => damage.Landed))
        {
            DelayedEffects.Occur(
                world, "WhenDamageDealt", damage.Target.ObjectId, events);
        }

        long dealt = placed.Sum(damage => damage.Dealt);
        bool damaged = placed.Any(damage => damage.Landed);

        if (damaged)
        {
            if (world.Attack is { } currentAttack)
            {
                world.Attack = currentAttack with { Damaged = true };
            }
            else if (world.FinishedAttack is { } finishedAttack)
            {
                world.FinishedAttack = finishedAttack with { Damaged = true };
            }
            occurrence.Also(Steps.DamageDealt);
        }
        if (world.Activation is { } activation)
        {
            world.Activation = activation with { DamageDealt = activation.DamageDealt + dealt };
        }
        else if (world.FinishedActivation is { } finishedActivation)
        {
            world.FinishedActivation = finishedActivation with
            {
                DamageDealt = finishedActivation.DamageDealt + dealt,
            };
        }

        if (FinishIndirectDefeats(
                world, facts, attacker,
                [.. placed.Where(damage => damage.Landed)
                    // Identity elimination clears that player's whole play
                    // area. Finish every ally's simultaneous damage sequence
                    // first so cleanup cannot erase its defeat and callbacks.
                    .OrderBy(damage => world.Seats.Any(seat =>
                        seat.IdentityCard.ObjectId == damage.Target.ObjectId))
                    .Select(damage => damage.Target.ObjectId)],
                step.Character, occurrence, events))
        {
            return;
        }

        // Only the declared defender (or the undefended target) was attacked;
        // other recipients merely took damage from that attack.
        if (!Keywords.Has(world, attacker, Keywords.Ranged, facts))
        {
            Damage.Retaliate(world, facts, world.Cards[step.Character], attacker,
                Steps.AttackInitiated, events);
        }
    }

    /// <summary>Resolve retaliation after an indirect-damage defeat decision.</summary>
    public static void FinishIndirectDamage(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var attacker = world.Cards[step.Subject];
        var occurrence = step.ProcedureOccurrence ?? world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "indirect attack damage continuation has no occurrence");
        if (FinishIndirectDefeats(
                world, facts, attacker, step.ProcedureCandidates ?? [],
                step.Character, occurrence, events))
        {
            return;
        }

        if (!Keywords.Has(world, attacker, Keywords.Ranged, facts))
        {
            Damage.Retaliate(world, facts, world.Cards[step.Character], attacker,
                Steps.AttackInitiated, events);
        }
    }

    private static bool FinishIndirectDefeats(
        World world, ICardFacts facts, Card attacker,
        IReadOnlyList<int> recipients, int attacked, Occurrence occurrence,
        List<GameEvent> events)
    {
        for (int index = 0; index < recipients.Count; index++)
        {
            var target = world.Cards[recipients[index]];
            if (!DeckTypes.IsInPlay(target.Area.Type))
            {
                continue;
            }

            var outcome = Damage.FinishPlaced(
                world, facts, attacker,
                new Damage.PlacedDamage(target, Dealt: 0, Taken: 1),
                Steps.AttackInitiated, "Attack", events,
                recordDefeatOn: occurrence);
            if (outcome != Damage.Outcome.Suspended)
            {
                continue;
            }

            world.Agenda.ThenContinuation(
                new PhaseStep(
                    Steps.FinishIndirectAttackDamage,
                    world.Agenda.Current?.Round ?? 0,
                    5,
                    Subject: attacker.ObjectId,
                    Character: attacked,
                    ProcedureCandidates: [.. recipients.Skip(index + 1)],
                    ProcedureOccurrence: occurrence,
                    Plan: true),
                occurrence);
            return true;
        }

        return false;
    }

    private static List<Card> IndirectCandidates(World world, ICardFacts facts, int player) =>
    [
        .. world.Cards
            .Where(card => card.Area.PlayArea == PlayArea.Of(player))
            .Where(card => card.ObjectId == world.Seats[player].IdentityCard.ObjectId
                || FacedownDrones.Kind(card, facts) == CardKind.Ally)
            .Where(card => DeckTypes.IsInPlay(card.Area.Type)
                && Damage.Health(world, facts, card) - card.Damage > 0
                && world.DamageAbilities.CanTakeDamage(world, card, world.Cards[Current(world).Enemy]))
            .OrderBy(card => card.ObjectId),
    ];

    /// <summary>
    /// How much damage the attack deals —
    /// <c>rr:attack-enemy-activation.step.4</c>.
    /// </summary>
    /// <remarks>
    /// Named for the number rather than the noun so that it does not shadow
    /// <see cref="Play.Damage"/>, which is the rule for dealing it. This step
    /// works out an amount; that one applies it.
    /// </remarks>
    /// <remarks>
    /// "The base damage is equal to the attacking enemy's ATK, including
    /// modifiers from abilities in play and boost icons resolved for the attack.
    /// If a hero has been declared the defender of the attack, reduce the amount
    /// of damage dealt by that hero's DEF value."
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="attack">The attack.</param>
    public static long Amount(World world, ICardFacts facts, EnemyAttack attack)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(attack);

        long amount = StateFields.Modified(
            world, world.Cards[attack.Enemy], "attack", facts, world.Players);

        if (attack.BasicDefense)
        {
            amount -= StateFields.Modified(
                world, world.Cards[attack.Defender], "defense", facts, world.Players);
        }

        return Math.Max(0, amount);
    }

    /// <summary>
    /// Step 6. The attack finishes resolving —
    /// <c>rr:attack-enemy-activation.step.6</c>.
    /// </summary>
    /// <remarks>
    /// Everything bounded by this attack ends here (<c>rr:lasting-effects.5</c>)
    /// and everything delayed until it resolves here
    /// (<c>rr:delayed-effect.1</c>), which is where Charge discards itself and
    /// the overkill it granted goes away. The abilities <c>.step.6.a</c> and
    /// <c>.step.6.b</c> list are the two windows around this step, so they need
    /// no code of their own.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void End(World world, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        if (world.Attack is null)
        {
            return;
        }

        Finish(world, events);
    }

    /// <summary>Ends an attack immediately when its attacked player is eliminated.</summary>
    public static void EndForEliminatedPlayer(World world, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);
        if (world.Attack is null)
        {
            return;
        }

        int activationId = world.Activation?.Id ?? -1;
        world.Agenda.Occurrence?.Also(Steps.AttackEnds);
        world.Agenda.EndActivationEarly(activationId);
        Finish(world, events);
    }

    private static void Finish(World world, List<GameEvent> events)
    {
        world.Effects.Expire(TimingPoints.EndOfAttack, events);

        // An attack is one of the two kinds of activation -- `rr:activation` --
        // so anything bounded by "this activation" ends here too, and there is
        // no longer an activating enemy for a card to name.
        world.Effects.Expire(TimingPoints.EndOfActivation, events);
        world.FinishedActivation = world.Activation;
        world.Activation = null;
        DelayedEffects.Occur(world, Steps.AttackEnds, events);

        // Kept for the window that follows this step. `.step.6.a`'s abilities
        // are the ones that ask what the attack did, and they run after the
        // attack is over -- so clearing `Attack` without keeping the facts
        // would leave them nothing to read.
        world.FinishedAttack = world.Attack;
        world.Attack = null;
    }

    /// <summary>
    /// Whether the attacking enemy has left play — <c>rr:activation.6</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "If an activating minion <b>leaves play</b>, that minion's activation
    /// ends immediately and <b>no further steps of that activation
    /// resolve</b>." An attack is six steps on the agenda and any of them can
    /// defeat the attacker — retaliate does it, and so does an interrupt that
    /// answers the attack by killing the thing making it.
    /// </para>
    /// <para>
    /// <c>rr:in-play-and-out-of-play.2</c> is what "in play" means for an
    /// encounter card, and a defeated minion is in the encounter discard pile.
    /// Without this a minion attacked from the discard pile just as hard as one
    /// on the table.
    /// </para>
    /// <para>
    /// <b>Checked at each step rather than once at the start.</b> An enemy that
    /// was already gone when the activation was scheduled and one defeated
    /// half-way through it are the same case, and one guard answers both --
    /// which is why <see cref="Initiate"/> has none of its own.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <returns>True when the rest of the activation must not resolve.</returns>
    public static bool Over(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        return world.Activation is not { } activation
            || !DeckTypes.IsInPlay(world.Cards[activation.Enemy].Area.Type);
    }

    /// <summary>The activation these steps belong to, of either kind.</summary>
    /// <remarks>
    /// <c>rr:activation</c>: an attack and a scheme are both activations, and
    /// steps 1 and 3 of an attack are word-for-word steps 1 and 2 of a scheme.
    /// So the boost-card steps read the umbrella — which enemy, against which
    /// seat — and only reach for <see cref="EnemyAttack"/> where they need
    /// something an attack has and a scheme does not.
    /// </remarks>
    private static EnemyActivation Activating(World world) =>
        world.Activation
        ?? throw new RulesNotImplementedException("no enemy is activating");

    /// <summary>Which condition this activation's boost cards are recorded under.</summary>
    /// <remarks>
    /// The two kinds keep their own names on the wire. <c>Steps.AttackInitiated</c>
    /// and <c>Steps.EnemySchemes</c> are separate conditions because a card can
    /// name either one, and a boost card taken off the encounter deck during a
    /// scheme was not taken during an attack.
    /// </remarks>
    private static string Activated(EnemyActivation activation) =>
        activation.Attacking ? Steps.AttackInitiated : Steps.EnemySchemes;

    /// <summary>Where an enemy's facedown boost cards wait.</summary>
    private static Area BoostCards(World world, int enemy) =>
        world.AreaOf(DeckType.BoostCardsDeck, world.Cards[enemy].Area.PlayArea, host: enemy);

    /// <summary>The characters one player could exhaust to defend.</summary>
    private static List<Card> Defenders(World world, ICardFacts facts)
    {
        var candidates = new List<Card>();

        // `rr:defend-defense.5` -- **every** player's characters, not just the
        // attacked one's. "Only one player at a time can defend" (`.1`) is a
        // limit on the answer, not on the offer, and the choice is one prompt
        // whose affordances carry whose character each is.
        foreach (int player in world.PlayerOrder)
        {
            candidates.AddRange(For(world, facts, player));
        }

        return candidates;
    }

    /// <summary>Applies card-specific defender constraints to the rules candidates.</summary>
    private static DefenderChoice Choice(
        World world, ICardFacts facts, IAttackCardAbilities abilities, EnemyAttack attack)
    {
        List<Card> legal;
        if (attack.IsDefended)
        {
            var current = world.Cards[attack.Defender];
            legal = !attack.BasicDefense
                && current.Ready
                && FacedownDrones.Kind(current, facts) == CardKind.Hero
                && BasicPowers.CanUsePower(facts, current, "DEF")
                    ? [current]
                    : [];
        }
        else
        {
            legal = Defenders(world, facts);
        }
        var choice = abilities.Defenders(world, attack, legal);
        if (choice.Required && choice.Candidates.Count == 0)
        {
            throw new RulesNotImplementedException(
                $"card {attack.Enemy} requires a defender but offers no legal candidate");
        }

        var legalIds = legal.Select(card => card.ObjectId).ToHashSet();
        if (choice.Candidates.Any(card => !legalIds.Contains(card.ObjectId)))
        {
            throw new RulesNotImplementedException(
                $"card {attack.Enemy} offered a character that cannot defend");
        }

        return choice;
    }

    /// <summary>One player's characters that could defend.</summary>
    private static List<Card> For(World world, ICardFacts facts, int player)
    {
        var seat = world.Seats[player];

        var candidates = new List<Card>();
        var identity = seat.IdentityCard;

        // rr:defend-defense.2 -- the basic defense power belongs to a hero. An
        // alter-ego has no DEF and cannot make one, and an exhausted hero has
        // nothing left to exhaust.
        if (identity.Ready
            && facts.Kind(identity.FaceId) == CardKind.Hero
            && BasicPowers.CanUsePower(facts, identity, "DEF"))
        {
            candidates.Add(identity);
        }

        // rr:defend-defense.3 -- "an ally can exhaust to defend against an
        // enemy attack. Damage from the attack is dealt to that ally."
        candidates.AddRange(world.Areas
            .Where(area => area.Type == DeckType.AlliesArea
                && area.PlayArea == PlayArea.Of(player))
            .SelectMany(area => area.Cards)
            .Where(ally => ally.Ready));

        return candidates;
    }

    private static EnemyAttack Current(World world) =>
        world.Attack
        ?? throw new RulesNotImplementedException("no attack is being resolved");

    /// <summary>Make an attack undefended when its defending ally has left play.</summary>
    /// <remarks>
    /// <c>rr:attack-enemy-activation.3.2</c>: if the defending ally leaves
    /// before attack damage is dealt, the attack has no defending character
    /// and the identity of that ally's controller becomes its target. The
    /// controller was captured as <see cref="EnemyAttack.Player"/> when the
    /// ally defended; after the ally moves, its discard-pile area records its
    /// owner instead and is too late to answer that rules question.
    /// </remarks>
    public static void RefreshDefender(World world, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        if (world.Attack is not { } attack)
        {
            return;
        }

        if (attack.Defender < 0)
        {
            return;
        }

        var defender = world.Cards[attack.Defender];
        if (FacedownDrones.Kind(defender, facts) != CardKind.Ally
            || DeckTypes.IsInPlay(defender.Area.Type))
        {
            return;
        }

        if (world.Effects.Active().Any(effect =>
            effect.Kind == AttackDamageResolved
            && effect.Affects == attack.Target))
        {
            return;
        }

        var fallback = world.Effects.Active().LastOrDefault(effect =>
            effect.Kind == DefenseFallback
            && effect.Affects == defender.ObjectId);
        if (fallback is { Amount: >= 0 and <= int.MaxValue })
        {
            int fallbackId = (int)fallback.Amount;
            var fallbackDefender = world.Cards[fallbackId];
            if (DeckTypes.IsInPlay(fallbackDefender.Area.Type))
            {
                world.Attack = attack with
                {
                    Defender = fallbackId,
                    Target = fallbackId,
                    Player = fallbackDefender.Area.PlayArea.Player,
                    BasicDefense = false,
                };
                return;
            }
        }

        var retargeted = attack with
        {
            Defender = -1,
            Target = world.Seats[attack.Player].IdentityCard.ObjectId,
            BasicDefense = false,
        };
        world.Attack = retargeted;
    }
}
