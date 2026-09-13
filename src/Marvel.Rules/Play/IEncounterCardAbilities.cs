using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text needed while resolving an encounter card.</summary>
public interface IEncounterCardAbilities
{
    IReadOnlyList<GameEvent> EntersPlay(World world, Card card);
    IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player);
    IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player, Occurrence occurrence);
    IReadOnlyList<PendingAbility> WhenRevealedAbilities(World world, Card card, int player);
    bool CancelWhenRevealed(World world, Card card, int player, Occurrence occurrence);
    IReadOnlyList<GameEvent> Boost(World world, Card card, int player);
}
#pragma warning restore CS1591
