using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text needed while resolving an encounter card.</summary>
public interface IEncounterCardAbilities
{
    AbilityExecutionResult EntersPlay(World world, AbilityExecution entry, Card card);
    AbilityExecutionResult WhenRevealed(World world, AbilityExecution entry, Card card, int player);
    IReadOnlyList<PendingAbility> WhenRevealedAbilities(World world, Card card, int player);
    WhenRevealedCanceled? CancelWhenRevealed(World world, Card card, int player, Occurrence occurrence);
}
#pragma warning restore CS1591
