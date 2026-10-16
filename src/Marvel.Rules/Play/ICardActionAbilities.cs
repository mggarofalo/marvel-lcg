using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card actions and their player-facing descriptions.</summary>
public interface ICardActionAbilities : IAbilityDescriptions
{
    IReadOnlyList<PendingAbility> Actions(World world, int player);
    AbilityExecutionResult Act(World world, AbilityExecution invocation, Decision answer);
}
#pragma warning restore CS1591
