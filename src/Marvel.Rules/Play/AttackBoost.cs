using static Marvel.Rules.Play.Attack;
using static Marvel.Rules.Play.AttackBoost;
using static Marvel.Rules.Play.AttackDamage;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

internal static class AttackBoost
{
    public static void GiveBoostCard(World world, ICardFacts facts, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:activation.6` -- "if an activating minion leaves play, that
        // minion's activation ends immediately and no further steps of that
        // activation resolve."
        if (AttackCompletion.Over(world))
        {
            return;
        }

        var activation = AttackCompletion.Activating(world);
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

    internal static void DealBoostCard(
        World world, Card enemy, string trigger, List<GameEvent> events)
    {
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var boost = EncounterDeck.TakeTop(world, trigger, events);
        if (boost is null)
        {
            return;
        }

        var onto = AttackCompletion.BoostCards(world, enemy.ObjectId);
        onto.Append(boost);
        events.Add(new CardsMoved(
            Places.Reference(deck), Places.Reference(onto),
            [new Landing(boost.ObjectId, onto.Cards.Count - 1)])
        {
            Trigger = trigger,
            Verb = "Boost",
        });
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
        if (AttackCompletion.Over(world))
        {
            return;
        }

        var activation = AttackCompletion.Activating(world);
        var waiting = AttackCompletion.BoostCards(world, activation.Enemy);
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
            Trigger = trigger,
            Verb = "Boost",
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
        if (!MatchesActivation(world, step, out var activation))
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

    private static bool MatchesActivation(
        World world, PhaseStep step, out EnemyActivation activation)
    {
        activation = world.Activation!;
        return activation is not null && activation.Enemy == step.Character
            && activation.Player == step.Seat && activation.Attacking == step.ProcedureFlag
            && DeckTypes.IsInPlay(world.Cards[step.Character].Area.Type);
    }

    internal static void DiscardBoostCard(
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
            Trigger = trigger,
            Verb = "Boost",
        });
    }

}
