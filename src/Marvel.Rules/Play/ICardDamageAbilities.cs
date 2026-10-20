using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text needed by the damage procedure.</summary>
public interface ICardDamageAbilities
{
    bool CanTakeDamage(World world, Card target, Card source);
    DamageProjection PreviewDamageReplacement(World world, Card target, Card source, long amount);
    DefeatProjection? PreviewDefeatReplacement(World world, Card target, long maximumHealth);
    AbilityExecutionResult WouldBeDealt(World world, AbilityExecution entry, DamageReplacement replacement);
    long WouldTake(World world, Card target, Card source, long amount, List<GameEvent> events);
    void DamagePreventedByTough(World world, Card target, Card source, List<GameEvent> events);
    AbilityExecutionResult WouldBeDefeated(World world, AbilityExecution entry,
        Card target, DefeatCause cause);
    AbilityExecutionResult WhenCardsDefeated(World world, AbilityExecution entry,
        IReadOnlyList<Defeated> defeated, string trigger);
    AbilityExecutionResult WhenCardDefeated(World world, AbilityExecution entry, Card card,
        Defeated defeated, string trigger);
}
#pragma warning restore CS1591
