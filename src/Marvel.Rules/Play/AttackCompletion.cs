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

    /// <summary>The characters one player could exhaust to defend.</summary>
    internal static List<Card> Defenders(World world, ICardFacts facts)
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
    internal static DefenderChoice Choice(
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
    internal static List<Card> For(World world, ICardFacts facts, int player)
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
