using static Marvel.Rules.Play.Attack;
using static Marvel.Rules.Play.AttackBoost;
using static Marvel.Rules.Play.AttackDamage;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

internal static class AttackCompletion
{
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

    internal static void Finish(World world, List<GameEvent> events)
    {
        var attack = Current(world);
        var subjects = new[] { attack.Enemy, attack.Target, attack.Defender }
            .Where(id => id >= 0).Distinct().ToDictionary(id => id,
                id => SubjectTitle(world, world.Facts, id));

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

        // rr:attack-enemy-activation.step.6: "The attack ends." Completion
        // and its defender are established facts, even when no field changed.
        // The emitted event shape and completion verb are engine choices
        // outside digest v3; completion is separate from an attack effect.
        events.Add(new AttackCompleted(attack.Enemy, attack.Target, attack.Defender)
        {
            DamageDealt = world.FinishedActivation?.DamageDealt,
            Subjects = subjects,
            Trigger = Steps.AttackEnds,
            Verb = "Attack_Completed",
        });
    }

    internal static string SubjectTitle(World world, ICardFacts facts, int id)
    {
        // A temporary title belongs to the occurrence even after departure
        // restores the physical card's printed identity.
        if (world.Agenda.Current?.ProcedureOwnerOccurrence is
            { Actor: var actor, ActorFacts: { EffectiveTitle: { } title } }
            && actor == id)
        {
            return title;
        }
        return EffectiveCards.Title(world.Cards[id], facts);
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
    internal static EnemyActivation Activating(World world) =>
        world.Activation
        ?? throw new RulesNotImplementedException("no enemy is activating");

    /// <summary>Which condition this activation's boost cards are recorded under.</summary>
    /// <remarks>
    /// The two kinds keep their own names on the wire. <c>Steps.AttackInitiated</c>
    /// and <c>Steps.EnemySchemes</c> are separate conditions because a card can
    /// name either one, and a boost card taken off the encounter deck during a
    /// scheme was not taken during an attack.
    /// </remarks>
    internal static string Activated(EnemyActivation activation) =>
        activation.Attacking ? Steps.AttackInitiated : Steps.EnemySchemes;

    /// <summary>Where an enemy's facedown boost cards wait.</summary>
    internal static Area BoostCards(World world, int enemy) =>
        world.AreaOf(DeckType.BoostCardsDeck, world.Cards[enemy].Area.PlayArea, host: enemy);

    internal static EnemyAttack Current(World world) =>
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
        if (EffectiveCards.Kind(defender, facts) != CardKind.Ally
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
