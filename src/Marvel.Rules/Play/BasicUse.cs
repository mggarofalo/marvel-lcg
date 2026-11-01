using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One incarnation-bound basic power and its local modifier.</summary>
public sealed class BasicUse(int actor, int incarnation, string field = "attack")
{
    /// <summary>The exact physical performer.</summary>
    public int Actor { get; } = actor;
    /// <summary>The performer incarnation when the use began.</summary>
    public int Incarnation { get; } = incarnation;
    /// <summary>The matching modified field for this basic use.</summary>
    public string Field { get; } = field;

    /// <summary>Whether the exact performer remains in play.</summary>
    public bool IsCurrent(World world) => world.Cards[Actor] is var card
        && card.Incarnation == Incarnation && DeckTypes.IsInPlay(card.Area.Type);
    /// <summary>Additional power for only this occurrence.</summary>
    public long Bonus { get; private set; }

    /// <summary>Whether this exact attack gained overkill.</summary>
    public bool Overkill { get; private set; }

    /// <summary>Grant overkill to this occurrence without changing the character.</summary>
    public void GrantOverkill()
    {
        if (Field != "attack") throw new InvalidOperationException("overkill requires a basic attack");
        Overkill = true;
    }

    /// <summary>Add a checked positive modifier to this exact use.</summary>
    public void Increase(long amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        Bonus = checked(Bonus + amount);
    }

    internal static BasicUse? ForAttack(World world, ICardFacts facts, CharacterAttack attack) =>
        attack.Amount < 0 && attack.Source < 0 && attack.Continuation is null
            ? new(attack.Attacker, world.Cards[attack.Attacker].Incarnation) : null;

    internal static BasicUse? ForThwart(World world, CharacterThwart thwart) =>
        thwart.Amount < 0 && thwart.Source < 0 && thwart.Continuation is null
            ? new(thwart.Thwarter, world.Cards[thwart.Thwarter].Incarnation, "thwart") : null;

    internal static IReadOnlyList<string> ThwartConditions(World world, ICardFacts facts,
        CharacterThwart thwart, IReadOnlyList<string> ordinary) =>
        ForThwart(world, thwart) is null ? ordinary
            : EffectiveCards.Kind(world.Cards[thwart.Thwarter], facts) switch
            {
                CardKind.Hero => [.. ordinary, BasicPowerTiming.HeroThwartInitiated, BasicPowerTiming.HeroUseInitiated],
                CardKind.Ally => [.. ordinary, BasicPowerTiming.AllyUseInitiated],
                _ => ordinary,
            };

    internal static IReadOnlyList<string> AttackConditions(World world, ICardFacts facts,
        CharacterAttack attack, IReadOnlyList<string> ordinary) =>
        ForAttack(world, facts, attack) is null ? ordinary
            : EffectiveCards.Kind(world.Cards[attack.Attacker], facts) switch
            {
                CardKind.Hero => [.. ordinary, BasicPowerTiming.HeroAttackInitiated, BasicPowerTiming.HeroUseInitiated],
                CardKind.Ally => [.. ordinary, BasicPowerTiming.AllyAttackInitiated, BasicPowerTiming.AllyUseInitiated],
                _ => ordinary,
            };
}
