using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// Dealing damage to a character — <c>rr:damage</c>.
/// </summary>
/// <remarks>
/// <para>
/// One path, because <c>rr:damage</c> is one rule however the damage arrived.
/// An enemy attacking a hero, a hero attacking a minion and a card ability
/// dealing 1 all reduce remaining hit points the same way and all check for
/// defeat the same way.
/// </para>
/// <para>
/// <c>rr:hit-points.2</c> and <c>.3</c> describe two different bookkeepings —
/// a dial for identities and villains, tokens for allies and minions — but
/// they are the same arithmetic and the digest records the result of it,
/// <c>health</c>, for all four. See <c>Card.Damage</c>.
/// </para>
/// </remarks>
public static class Damage
{
    /// <inheritdoc cref="DamagePlacement.Health"/>
    public static long Health(World world, ICardFacts facts, Card character) =>
        DamagePlacement.Health(world, facts, character);

    /// <summary>Resolves an attack whose attacker is also its damage source.</summary>
    public static AttackDamageResult Attack(
        World world, ICardFacts facts, Card attacker, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, bool retaliate = true) =>
        DamageAttacks.Attack(
            world, facts, attacker, target, amount, trigger, verb, events, retaliate);

    /// <summary>Resolves an attack with a distinct damage source.</summary>
    public static AttackDamageResult Attack(
        World world, ICardFacts facts, Card attacker, Card source, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, bool retaliate = true) =>
        DamageAttacks.Attack(
            world, facts, attacker, source, target, amount, trigger, verb, events, retaliate);

    /// <inheritdoc cref="DamageAttacks.Retaliate"/>
    public static void Retaliate(
        World world, ICardFacts facts, Card attacked, Card attacker,
        string trigger, List<GameEvent> events) =>
        DamageAttacks.Retaliate(world, facts, attacked, attacker, trigger, events);

    /// <inheritdoc cref="DamageRecovery.Heal"/>
    public static long Heal(
        World world, ICardFacts facts, Card target, long amount,
        string trigger, string verb, List<GameEvent> events) =>
        DamageRecovery.Heal(world, facts, target, amount, trigger, verb, events);

    /// <inheritdoc cref="DamageRecovery.MoveDamage"/>
    public static long MoveDamage(
        World world, ICardFacts facts, Card source, Card from, Card to, long amount,
        string trigger, string verb, List<GameEvent> events, int by = -1) =>
        DamageRecovery.MoveDamage(
            world, facts, source, from, to, amount, trigger, verb, events, by);

    /// <summary>Whether dealing damage finished, defeated, or suspended.</summary>
    public enum Outcome
    {
        /// <summary>The damage procedure finished without defeating the target.</summary>
        NotDefeated,
        /// <summary>The damage procedure finished and defeated the target.</summary>
        Defeated,
        /// <summary>The damage is placed but a defeat decision remains outstanding.</summary>
        Suspended,
    }

    /// <summary>Describes the presently knowable result of committing one attack.</summary>
    /// <remarks>
    /// This engine-owned preview follows constant prohibitions, forced replacement,
    /// Piercing, Tough and active prevention. Optional future windows and
    /// would-be-defeated abilities remain future decisions rather than guesses.
    /// </remarks>
    public static string PreviewAttack(
        World world, ICardFacts facts, Card attacker, Card source, Card target, long amount,
        bool grantsOverkill = false) =>
        new DamageConsequencePreview(world, facts, source, target)
            .Describe(attacker, amount, isAttack: true, grantsOverkill);

    /// <summary>Describes the consequence of a character's basic ATK power.</summary>
    public static string PreviewBasicAttack(World world, ICardFacts facts, Card attacker, Card target, long amount) =>
        new DamageConsequencePreview(world, facts, attacker, target, basic: true)
            .Describe(attacker, amount, isAttack: true, grantsOverkill: false);

    /// <summary>Describes presently knowable non-attack damage consequences.</summary>
    public static string PreviewDamage(
        World world, ICardFacts facts, Card source, Card target, long amount) =>
        new DamageConsequencePreview(world, facts, source, target)
            .Describe(source, amount, isAttack: false, grantsOverkill: false);

    /// <summary>Damage fixed through step 4, ready for simultaneous placement.</summary>
    internal sealed record PlacedDamage(Card Target, long Dealt, long Taken)
    {
        internal int Incarnation { get; } = Target.Incarnation;
        /// <summary>Whether the character actually took damage.</summary>
        public bool Landed => Taken > 0;
    }
}
