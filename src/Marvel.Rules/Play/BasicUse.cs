using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One incarnation-bound basic attack and its local modifier.</summary>
public sealed class BasicUse(int actor, int incarnation)
{
    /// <summary>The exact physical performer.</summary>
    public int Actor { get; } = actor;
    /// <summary>The performer incarnation when the use began.</summary>
    public int Incarnation { get; } = incarnation;
    /// <summary>The matching modified field for this basic use.</summary>
    public string Field { get; } = "attack";
    /// <summary>Additional power for only this occurrence.</summary>
    public long Bonus { get; private set; }

    /// <summary>Add a checked positive modifier to this exact use.</summary>
    public void Increase(long amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        Bonus = checked(Bonus + amount);
    }

    internal static BasicUse? ForAttack(World world, ICardFacts facts, CharacterAttack attack) =>
        attack.Amount < 0 && attack.Source < 0 && attack.Continuation is null
            ? new(attack.Attacker, world.Cards[attack.Attacker].Incarnation) : null;

    internal static IReadOnlyList<string> AttackConditions(World world, ICardFacts facts,
        CharacterAttack attack, IReadOnlyList<string> ordinary) =>
        ForAttack(world, facts, attack) is not null
            && EffectiveCards.Kind(world.Cards[attack.Attacker], facts) == CardKind.Hero
            ? [.. ordinary, BasicPowerTiming.HeroAttackInitiated] : ordinary;
}
