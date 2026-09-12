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
    long WouldBeDealt(World world, Card target, Card source, long amount, List<GameEvent> events);
    long WouldTake(World world, Card target, Card source, long amount, List<GameEvent> events);
    void DamagePreventedByTough(World world, Card target, Card source, List<GameEvent> events);
    void WouldBeDefeated(World world, Card target, List<GameEvent> events);
    bool WouldBeDefeated(World world, Card target, Card source, string trigger, string verb, int by,
        List<GameEvent> events, Occurrence? recordDefeatOn = null);
    IReadOnlyList<GameEvent> WhenCardDefeated(World world, Card card, Defeated defeated);
    bool WhenCardDefeated(World world, Card card, Defeated defeated, string trigger, List<GameEvent> events);
}
#pragma warning restore CS1591
