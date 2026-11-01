namespace Marvel.Cards.Dsl;

/// <summary>Effects that count the damage a player's identity deals to enemies.</summary>
public abstract record AbilityEnemyDamageEffect : AbilityEffect
{
    private AbilityEnemyDamageEffect() { }

    /// <summary>Heal the resolving identity each time it deals damage to an enemy until a timing point.</summary>
    public sealed record HealOnEnemyDamage(long Amount, string Until) : AbilityEnemyDamageEffect;
    /// <summary>Increase the imminent excess damage of an attack.</summary>
    public sealed record IncreaseExcessDamage(long Amount) : AbilityEnemyDamageEffect;
}
