using static Marvel.Rules.Play.Attack;
using static Marvel.Rules.Play.AttackBoost;
using static Marvel.Rules.Play.AttackDamage;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

internal static class AttackDamage
{
    public static void CalculateDamage(World world, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        if (AttackCompletion.Over(world))
        {
            return;
        }

        AttackCompletion.RefreshDefender(world, facts);
        var attack = AttackCompletion.Current(world);
        world.Attack = attack with { CalculatedDamage = AttackCompletion.Amount(world, facts, attack) };
    }

    /// <summary>Make the current enemy attack deal indirect damage.</summary>
    public static void MakeIndirect(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.Attack = AttackCompletion.Current(world) with { Indirect = true };
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
        if (AttackCompletion.Over(world))
        {
            return;
        }

        AttackCompletion.RefreshDefender(world, facts);
        var attack = AttackCompletion.Current(world);
        long amount = attack.CalculatedDamage
            ?? throw new RulesNotImplementedException(
                "attack damage reached step 5 before step 4 calculated it");

        // The departure rule applies only before attack damage is dealt. The
        // step is resolved even when its calculated amount is zero, so mark it
        // before that early return. The marker also prevents a nested damage
        // consequence from rewriting the attack after assignment has begun.
        MarkDamageStepResolved(world, attack);
        if (amount <= 0)
        {
            return;
        }

        if (attack.Indirect)
        {
            IndirectAttackAssignment.BeginIndirectDamage(world, facts, attack, amount, events);
            return;
        }

        // Through the same primitive a hero's basic attack uses. `rr:damage` is
        // one rule however the damage arrived, and so is `rr:defeat` -- an
        // enemy attack that defeated a character down a separate path would be
        // a second place for the defeat rules to be wrong.
        // One call, because `rr:piercing`, `rr:overkill` and `rr:ranged` are all
        // properties of the attack rather than of either character.
        int firstDamageEvent = events.Count;
        var damage = DamageAttacks.Attack(
            world, facts, world.Cards[attack.Enemy], world.Cards[attack.Target], amount,
            Steps.AttackInitiated, "Deal_Damage", events);

        // Recorded on the attack rather than derived later, because by the time
        // `rr:attack-enemy-activation.step.6.a`'s abilities run the damage is on
        // a dial that had damage on it before. `damaged` is the list
        // `rr:tough.3` shortens -- a character whose tough card absorbed the
        // attack "is not considered to have taken damage" -- so an attack that
        // hit a tough card did not damage anybody.
        AttackDamageAccounting.RecordAttackDamage(world, attack, damage, events, firstDamageEvent);
    }

    private static void MarkDamageStepResolved(World world, EnemyAttack attack) =>
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: AttackDamageResolved,
            Affects: attack.Target,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfAttack)));

    /// <summary>Ask the attacked player to assign indirect attack damage.</summary>
    public static Prompt IndirectDamagePrompt(World world, ICardFacts facts, PhaseStep step) =>
        IndirectAttackAssignment.IndirectDamagePrompt(world, facts, step);

    /// <summary>Accept the player's indirect attack assignment.</summary>
    public static void AssignIndirectDamage(World world, ICardFacts facts, PhaseStep step, Decision input, List<GameEvent> events) =>
        IndirectAttackAssignment.AssignIndirectDamage(world, facts, step, input, events);

    /// <summary>Prepare one assigned recipient's damage.</summary>
    public static long PrepareIndirectDamage(World world, PhaseStep step, List<GameEvent> events) =>
        IndirectAttackResolution.PrepareIndirectDamage(world, step, events);

    /// <summary>Place all assigned shares simultaneously.</summary>
    public static void ApplyIndirectDamage(World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) =>
        IndirectAttackResolution.ApplyIndirectDamage(world, facts, step, events);

    /// <summary>Finish an indirect damage continuation.</summary>
    public static void FinishIndirectDamage(World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) =>
        IndirectAttackResolution.FinishIndirectDamage(world, facts, step, events);

    internal static bool FinishIndirectDefeats(World world, ICardFacts facts, Card attacker,
        IReadOnlyList<int> recipients, int attacked, Occurrence occurrence, List<GameEvent> events) =>
        IndirectAttackResolution.FinishIndirectDefeats(world, facts, attacker, recipients, attacked, occurrence, events);

    internal static List<Card> IndirectCandidates(World world, ICardFacts facts, int player) =>
        IndirectAttackAssignment.IndirectCandidates(world, facts, player);
}
