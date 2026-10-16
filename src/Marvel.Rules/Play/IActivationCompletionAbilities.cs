using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text waiting for an enemy activation to complete.</summary>
public interface IActivationCompletionAbilities
{
    AbilityExecutionResult ActivationCompleted(World world, AbilityExecution entry, EnemyActivation result);
}
#pragma warning restore CS1591
