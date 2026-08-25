using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Content.Cards;

/// <summary>
/// The card behaviour a recorded transition actually reaches, written out.
/// </summary>
/// <remarks>
/// <para>
/// <b>A placeholder with an expiry date, and it is worth being blunt about
/// that.</b> <c>docs/migration.md</c> settled that cards become data rather
/// than code, and <c>docs/card-dsl.md</c> designs the interpreter that runs
/// them — opening with "nothing here is implemented". This is what stands in
/// until it exists.
/// </para>
/// <para>
/// It is not a parallel path, because there is exactly one way a card's
/// behaviour enters the engine — <see cref="ICardAbilities"/> — and the
/// interpreter replaces what is behind it without the villain phase or the engine
/// changing. It is also deliberately tiny: <b>three cards</b>. Every card added
/// here before the interpreter exists is a card that has to be removed again, so
/// the rule is to add one only when something a test actually reaches needs it.
/// </para>
/// <para>
/// Two of the three are here for the same reason, and it is not the recording:
/// Charge and Spider-Man wait in the <b>same</b> interrupt window when Rhino
/// attacks, one forced and one optional, which is what makes the timing spine
/// load-bearing rather than merely cited. See <c>docs/enemy-attacks.md</c>.
/// </para>
/// <para>
/// Ported by reading the oracle's own implementations —
/// <c>py_src/cards/pack/core/rhino/01105.py</c>, <c>.../rhino/01099.py</c> and
/// <c>.../spider_man/01001a.py</c> — and then checked against the published
/// rules, which is the order that matters: the Python is evidence of intent and
/// the Rules Reference is the authority.
/// </para>
/// </remarks>
public sealed class CoreSetAbilities : ICardAbilities
{
    /// <summary>The printed id of "I'm Tough".</summary>
    public const string ImTough = "01105";

    /// <summary>The printed id of "Charge".</summary>
    public const string Charge = "01099";

    /// <summary>The printed id of Spider-Man's hero side.</summary>
    public const string SpiderMan = "01001a";

    /// <summary>The cards this knows about.</summary>
    /// <remarks>
    /// Public so a test can state the coverage gap as a set rather than
    /// discovering it as a failure. A recorded step that reveals anything else
    /// resolves to nothing here, which is why <see cref="WhenRevealed"/> throws
    /// rather than returning empty.
    /// </remarks>
    public static IReadOnlySet<string> Implemented { get; } =
        new HashSet<string>(StringComparer.Ordinal) { ImTough, Charge, SpiderMan };

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Two abilities, and they wait in the <b>same</b> window, which is why
    /// this pair is worth having: Rhino attacking is one occurrence with a
    /// forced interrupt and an optional one waiting in it, so
    /// <c>rr:forced.4</c> has something to order and <c>rr:ability.11</c>
    /// something to decline.
    /// </para>
    /// <para>
    /// Both are timed to the attack <i>initiating</i> rather than to any of its
    /// six steps. <c>rr:attack-enemy-activation.5</c>: interrupts that trigger
    /// "when [enemy name] attacks" have the same timing as those that trigger
    /// "when [the villain] initiates an attack".
    /// </para>
    /// </remarks>
    public IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);

        if (window != WindowKind.Interrupt || !occurrence.Is(Steps.EnemyAttacks))
        {
            return [];
        }

        var waiting = new List<PendingAbility>();

        // "Charge" (01099): `[star] Forced Interrupt: When Rhino attacks, the
        // attack gains overkill.` The star is in the card's ATK field, which
        // `rr:star-icon.2` makes a reminder "to check that attachment's text box
        // whenever the attached enemy uses the value that field is modifying to
        // attack" -- so this is an attachment ability while in play, not a
        // "Boost" ability.
        //
        // No controller: it is an encounter card, and `rr:ability.8` lets any
        // player resolve one. Nobody chooses here anyway -- it is forced.
        foreach (var card in world.Cards)
        {
            if (card.FaceId == Charge
                && DeckTypes.IsInPlay(card.Area.Type)
                && card.Area.Host == occurrence.Subject)
            {
                waiting.Add(new PendingAbility(
                    card.ObjectId, AbilityType.ForcedInterrupt, World.Scenario));
            }
        }

        // Spider-Man (01001a): `Spider-Sense -- Interrupt: When the villain
        // initiates an attack against you, draw 1 card.` "You" is the attacked
        // *player* (`rr:attack-enemy-activation.1.4`), and only in hero form,
        // because that face is the only one the ability is printed on.
        if (occurrence.Player >= 0 && occurrence.Player < world.Seats.Count)
        {
            var identity = world.Seats[occurrence.Player].IdentityCard;
            if (identity is not null
                && identity.FaceId == SpiderMan
                && DeckTypes.IsInPlay(identity.Area.Type))
            {
                waiting.Add(new PendingAbility(
                    identity.ObjectId, AbilityType.Interrupt, occurrence.Player));
            }
        }

        return waiting;
    }

    /// <inheritdoc/>
    public Affordance Describe(World world, PendingAbility ability)
    {
        ArgumentNullException.ThrowIfNull(world);

        var card = world.Cards[ability.Card];
        return card.FaceId switch
        {
            // The card's own name for the ability, which is what a player is
            // choosing between. `rr:labeled-ability`: a label before the dash
            // names the ability, and Spider-Man's is "Spider-Sense".
            SpiderMan => Offer(ability, "Spider-Sense"),
            Charge => Offer(ability, "Charge"),
            _ => throw new RulesNotImplementedException(
                $"card '{card.FaceId}' has no ability to describe in a window"),
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);

        var card = world.Cards[ability.Card];
        return card.FaceId switch
        {
            SpiderMan => SpiderSense(world, ability.Player),
            Charge => ChargeAttacks(world, occurrence, card),
            _ => throw new RulesNotImplementedException(
                $"card '{card.FaceId}' has no ability to resolve in a window"),
        };
    }

    // "Charge" (01099), the half that fires when Rhino attacks: "the attack
    // gains overkill [...] At the end of this attack, discard Charge."
    //
    // Both halves are bounded by the attack and neither happens now, so both
    // are registered rather than done. `rr:lasting-effects` calls "until the
    // end of this attack" a duration by name; `rr:delayed-effect.1` calls the
    // discard what it is, an effect waiting on a future condition.
    //
    // Nothing on the board changes at this moment, so there is no event. What
    // the client sees is the attack landing differently and Charge going to the
    // discard when it ends.
    private static IReadOnlyList<GameEvent> ChargeAttacks(
        World world, Occurrence occurrence, Card card)
    {
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: Keywords.Overkill,
            Card: card.ObjectId,
            Affects: occurrence.Subject,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfAttack)));

        world.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect,
            Kind: DelayedEffects.DiscardFromPlay,
            Card: card.ObjectId,
            Affects: card.ObjectId,
            Lasts: Duration.NextTime(Steps.AttackEnds)));

        return [];
    }

    // Spider-Man (01001a): "Spider-Sense -- Interrupt: When the villain
    // initiates an attack against you, draw 1 card."
    private static List<GameEvent> SpiderSense(World world, int player)
    {
        var drawn = new List<GameEvent>();
        Draw.Cards(world, player, 1, Steps.EnemyAttacks, drawn);
        return drawn;
    }

    // The ability's own name is the verb, which is the engine's convention and
    // not a guess: `datasets/digest/prompts.json` offers `Foresight` and
    // `"I_Object!"` as verbs, both card names. One string does for both fields
    // because the engine carries one -- see the remarks on `Affordance.Id`.
    private static Affordance Offer(PendingAbility ability, string name) =>
        new(Id: ability.Card,
            Verb: name,
            AnchorId: ability.Card,
            AnchorPlayer: ability.Player,
            Label: name);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        return card.FaceId switch
        {
            ImTough => ImToughRevealed(world, card),
            Charge => ChargeRevealed(world, card),
            _ => throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was revealed and no ability is written for it; "
                + $"this engine knows {Implemented.Count} card(s), pending the interpreter"),
        };
    }

    // "Charge" (01099). Printed: "Attach to Rhino." The Python is
    // `AbilityFactory.AttachToFaceWhenPutIntoPlay(CardFinder(name="Rhino"))`,
    // which is the declarative half of the ability -- the imperative handler is
    // the Forced Interrupt that fires when Rhino *attacks*, and no recorded step
    // reaches it because the hero never leaves alter-ego form.
    //
    // The +3 attack is not here. It is `ATK+ 3` in the printed data, and
    // `StateFields` reads it off whatever is attached: a card that prints a
    // modifier does not need an ability to apply it.
    private static IReadOnlyList<GameEvent> ChargeRevealed(World world, Card card)
    {
        var villain = world.TheCardIn(DeckType.VillainArea);
        if (villain is null)
        {
            throw new RulesNotImplementedException(
                "\"Charge\" attaches to Rhino and there is no villain in play");
        }

        var onto = world.AreaOf(
            DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId,
            villain.Area.CardOwner);
        var from = card.Area;
        World.MoveToTop(card, onto);

        return
        [
            new CardsMoved(
                Places.Reference(from), Places.Reference(onto),
                [new Landing(card.ObjectId, onto.Cards.Count - 1)])
            {
                Trigger = "WhenCardRevealed", Verb = "Attach",
            },
            new CardAttached(card.ObjectId, villain.ObjectId)
            {
                Trigger = "WhenCardRevealed", Verb = "Attach",
            },
        ];
    }

    // "I'm Tough" (01105). The Python is:
    //
    //     villain = Worlds.FindCardOnField(effect, name="Rhino", card_type=Villain)
    //     if villain and not villain.IsTough():
    //         Faces.GiveStatus([villain], "Tough", effect)
    //     else:
    //         this.GainSurge(1, effect)
    //
    // The card names Rhino because it belongs to Rhino's encounter set and no
    // other villain can be on the table with it. Matching on "the villain in
    // play" rather than on the name is the same answer here and does not need a
    // name lookup the dataset spells differently.
    private static IReadOnlyList<GameEvent> ImToughRevealed(World world, Card card)
    {
        var villain = world.TheCardIn(DeckType.VillainArea);
        if (villain is null || Statuses.Has(world, villain, Statuses.Tough))
        {
            // Surge means "reveal an additional encounter card", which is a
            // villain-phase rule and not this card's. Left out rather than
            // guessed: the recorded round one takes the other branch, and a
            // surge that silently did nothing would deal one card too few for
            // the rest of the game.
            throw new RulesNotImplementedException(
                "\"I'm Tough\" would surge because the villain is already Tough; "
                + "surge is not implemented");
        }

        var status = Statuses.Give(world, villain, Statuses.Tough);
        return
        [
            new CardsCreated(
                Places.Reference(status.Area),
                [new CreatedCard(status.ObjectId, status.FaceId)])
            {
                Trigger = "WhenCardRevealed", Verb = "Give_Status",
            },
            new CardAttached(status.ObjectId, villain.ObjectId)
            {
                Trigger = "WhenCardRevealed", Verb = "Give_Status",
            },
        ];
    }
}
