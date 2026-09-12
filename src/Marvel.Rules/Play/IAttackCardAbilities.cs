using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text used by enemy attack resolution.</summary>
public interface IAttackCardAbilities
{
    DefenderChoice Defenders(World world, EnemyAttack attack, IReadOnlyList<Card> candidates);
    IReadOnlyList<GameEvent> Boost(World world, Card card, int player);
}
#pragma warning restore CS1591
