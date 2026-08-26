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
/// <para>
/// <b>What is not implemented, and why each is named rather than skipped.</b> A
/// minion attacking; a player other than the attacked one defending
/// (<c>rr:defend-defense.5</c>); an ally defending (<c>rr:defend-defense.3</c>);
/// a character being defeated by the damage (<c>rr:defeated</c>); overkill
/// actually carrying excess damage anywhere (<c>rr:overkill.1</c>, which needs
/// a defeat first). Each throws, because an attack that quietly skipped the
/// defence step would produce a board that is plausible and wrong.
/// </para>
/// </remarks>
public static class Attack
{
    /// <summary>
    /// The affordance verb for the basic defense power —
    /// <c>rr:defend-defense.2</c>.
    /// </summary>
    /// <remarks>
    /// Spelled as the oracle spells it. Its <c>Effect.GetDisplayName</c> names
    /// the four basic powers <c>Attack</c>, <c>Defense</c>, <c>Thwart</c> and
    /// <c>Recover</c>, and those strings are on the wire — a verb invented here
    /// would be a divergence in the half of the return value
    /// <c>datasets/digest/prompts.json</c> is there to check.
    /// </remarks>
    public const string DefenseVerb = "Defense";

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
            return;
        }

        // rr:attack-enemy-activation.1 -- against both a player and a
        // character. The character is the player's identity unless an ability
        // says otherwise, and none does here.
        var target = world.Seats[step.Seat].IdentityCard;
        world.Attack = new EnemyAttack(step.Subject, step.Seat, target.ObjectId);

        world.Agenda.Then(new PhaseStep(
            Steps.GiveBoostCard, step.Round, 1, Index: step.Seat, Subject: step.Subject));
        world.Agenda.Then(new PhaseStep(
            Steps.DeclareDefender, step.Round, 2, Index: step.Seat, Subject: step.Subject,
            Seat: step.Seat));
        world.Agenda.Then(new PhaseStep(
            Steps.FlipBoostCards, step.Round, 3, Index: step.Seat, Subject: step.Subject));
        world.Agenda.Then(new PhaseStep(
            Steps.DealAttackDamage, step.Round, 4, Index: step.Seat, Subject: step.Subject,
            Seat: step.Seat));
        world.Agenda.Then(new PhaseStep(
            Steps.EndAttack, step.Round, 6, Index: step.Seat, Subject: step.Subject,
            Seat: step.Seat));
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

        var attack = Current(world);
        var attacker = world.Cards[attack.Enemy];

        // `rr:attack-enemy-activation.step.1`: only a villain, or a minion with
        // `rr:villainous`, is given one. "If a minion without the villainous
        // keyword is attacking, **skip this step**" -- and skipping matters
        // beyond the icons, because taking a card off the encounter deck moves
        // every later deal.
        if (!Keywords.IsBoosted(attacker, facts, world.Players))
        {
            return;
        }

        var deck = world.AreaOf(DeckType.EncounterDeck);
        var boost = EncounterDeck.TakeTop(world, Steps.EnemyAttacks, events);
        if (boost is null)
        {
            return;
        }

        var onto = BoostCards(world, attack.Enemy);
        onto.Append(boost);
        events.Add(new CardsMoved(
            Places.Reference(deck), Places.Reference(onto),
            [new Landing(boost.ObjectId, onto.Cards.Count - 1)])
        {
            Trigger = Steps.EnemyAttacks, Verb = "Boost",
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
    /// <returns>The question, or null when nobody could defend.</returns>
    public static Prompt? DeclareDefender(World world, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        var attack = Current(world);
        var candidates = Defenders(world, facts);
        if (candidates.Count == 0)
        {
            return null;
        }

        var seat = world.Seats[attack.Player];
        return new Prompt(
            Player: attack.Player,
            Asking: Question.Defender,
            When: TimingPriority.Untimed,
            Trigger: Steps.EnemyAttacks,
            Label: $"{seat.Name} declares a defender",

            // rr:attack-enemy-activation.4 -- an attack with no defender is
            // undefended and resolves, so not defending is always an answer.
            Cancellable: true,
            Affordances:
            [
                // `AnchorPlayer` is whose character it is, which is not
                // always the player being asked: `rr:defend-defense.5` lets
                // somebody else's hero or ally defend, and taking over makes
                // them the attack's new target.
                .. candidates.Select(card => new Affordance(
                    Id: card.ObjectId,
                    Verb: DefenseVerb,
                    AnchorId: card.ObjectId,
                    AnchorPlayer: card.Owner,
                    Label: DefenseVerb)),
            ]);
    }

    /// <summary>The character that answer names becomes the defender.</summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="input">The player's answer.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Defend(
        World world, ICardFacts facts, Decision input, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(events);

        var attack = Current(world);
        if (input.IsDecline)
        {
            return;
        }

        var defender = Defenders(world, facts)
            .FirstOrDefault(card => card.ObjectId == input.Affordance)
            ?? throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered as a defender");

        // rr:defend-defense.2 and .3 -- both require exhausting the defender.
        defender.Exhaust();
        events.Add(new FieldSet(defender.ObjectId, "is_exhaust", 0, 1)
        {
            Trigger = Steps.EnemyAttacks, Verb = DefenseVerb,
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
            Player = defender.Owner,
            BasicDefense = facts.Kind(defender.FaceId) != CardKind.Ally,
        };
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
        World world, ICardFacts facts, ICardAbilities abilities, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(events);

        var attack = Current(world);
        var waiting = BoostCards(world, attack.Enemy);

        // "one at a time and in the order in which they were dealt", which is
        // bottom-first -- rr:attack-enemy-activation.step.3.
        while (waiting.Cards.Count > 0)
        {
            var boost = waiting.Cards[0];

            // Through the boosting area for the same reason a scheme's boost
            // card is: passing through is what registers the card's token
            // pools, and the discarded card's `k_threat` key is on the wire.
            World.MoveToTop(boost, world.AreaOf(DeckType.BoostingArea));
            events.Add(new CardsFlipped([boost.ObjectId], true)
            {
                Trigger = Steps.EnemyAttacks, Verb = "Boost",
            });

            // rr:attack-enemy-activation.step.3.c -- increase the enemy's ATK
            // by one per boost icon, for this attack. A bounded modifier is a
            // lasting effect (`rr:lasting-effects`), and "until the end of this
            // attack" is the rulebook's own example of a duration.
            // `rr:amplify-icon`: "when a boost card is turned faceup **during
            // an enemy activation**, add one additional boost icon to that card
            // for each amplify icon in play." Per card, so two boost cards with
            // one amplify icon in play gain one each.
            long icons = facts.PrintedValue(boost.FaceId, "Boost", world.Players)
                + MainScheme.Amplify(world, facts);
            if (icons > 0)
            {
                world.Effects.Register(new ContinuousEffect(
                    EffectSource.LastingEffect,
                    Kind: "attack",
                    Amount: icons,
                    Card: boost.ObjectId,
                    Affects: attack.Enemy,
                    Lasts: Duration.UntilEndOf(TimingPoints.EndOfAttack)));
            }

            // rr:attack-enemy-activation.step.3.b and rr:boost-boost-icon.2 --
            // a "Boost" ability under the divider line resolves here, while the
            // card is faceup in the boosting area and before step 3.d discards
            // it. The printed `Boost` attribute counts icons and cannot say
            // whether there is a star, so `ICardFacts.HasBoostAbility` reads the
            // text box, which is the only place the star survives.
            events.AddRange(abilities.Boost(world, boost, attack.Player));
            var discard = world.AreaOf(DeckType.EncounterDiscardPile);
            var from = boost.Area;
            World.MoveToTop(boost, discard);
            events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(discard),
                [new Landing(boost.ObjectId, discard.Cards.Count - 1)])
            {
                Trigger = Steps.EnemyAttacks, Verb = "Boost",
            });
        }
    }

    /// <summary>
    /// Steps 4 and 5. Work out the damage and deal it —
    /// <c>rr:attack-enemy-activation.step.4</c> and <c>.step.5</c>.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void DealDamage(World world, ICardFacts facts, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        var attack = Current(world);
        long amount = Amount(world, facts, attack);
        if (amount <= 0)
        {
            return;
        }

        // Through the same primitive a hero's basic attack uses. `rr:damage` is
        // one rule however the damage arrived, and so is `rr:defeat` -- an
        // enemy attack that defeated a character down a separate path would be
        // a second place for the defeat rules to be wrong.
        // One call, because `rr:piercing`, `rr:overkill` and `rr:ranged` are all
        // properties of the attack rather than of either character.
        var damaged = Damage.Attack(
            world, facts, world.Cards[attack.Enemy], world.Cards[attack.Target], amount,
            Steps.EnemyAttacks, "Deal_Damage", events);

        // `rr:delayed-effect.1` -- a delayed effect resolves "immediately after
        // [its] future condition occurs or becomes true, and **before responses
        // to that point or condition may be used**", which is why this is here
        // and not in the step's response window.
        //
        // Once per character actually damaged, and `rr:tough.3` is what makes
        // that list shorter than "who was attacked": a character whose tough
        // status card ate the damage "is not considered to have taken damage",
        // so "if a character is damaged by this attack" is false for them.
        foreach (var card in damaged)
        {
            DelayedEffects.Occur(world, "WhenDamageDealt", card.ObjectId, events);
        }
    }

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
            amount -= facts.PrintedValue(
                world.Cards[attack.Defender].FaceId, "DEF", world.Players);
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

        world.Effects.Expire(TimingPoints.EndOfAttack);
        DelayedEffects.Occur(world, Steps.AttackEnds, events);
        world.Attack = null;
    }

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

    /// <summary>One player's characters that could defend.</summary>
    private static List<Card> For(World world, ICardFacts facts, int player)
    {
        var seat = world.Seats[player];

        var candidates = new List<Card>();
        var identity = seat.IdentityCard;

        // rr:defend-defense.2 -- the basic defense power belongs to a hero. An
        // alter-ego has no DEF and cannot make one, and an exhausted hero has
        // nothing left to exhaust.
        if (identity.Ready && facts.Kind(identity.FaceId) == CardKind.Hero)
        {
            candidates.Add(identity);
        }

        // rr:defend-defense.3 -- "an ally can exhaust to defend against an
        // enemy attack. Damage from the attack is dealt to that ally."
        var allies = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player);
        candidates.AddRange(allies.Cards.Where(ally => ally.Ready));

        return candidates;
    }

    private static EnemyAttack Current(World world) =>
        world.Attack
        ?? throw new RulesNotImplementedException("no attack is being resolved");
}
