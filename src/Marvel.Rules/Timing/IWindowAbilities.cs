using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Discovers window abilities and executes an admitted game-owned invocation.</summary>
public interface IWindowAbilities : IAbilityDescriptions
{
    /// <summary>Whether an in-play authored ability observes player-turn beginnings.</summary>
    bool ObservesPlayerTurnBeginning(World world);
    /// <summary>Whether an in-play authored ability observes completion of villain step one.</summary>
    bool ObservesVillainStepOneCompletion(World world);

    /// <summary>Whether an authored in-play or hand ability observes hero defenses.</summary>
    bool ObservesHeroDefense(World world);

    /// <summary>The abilities currently eligible in this window.</summary>
    IReadOnlyList<PendingAbility> Waiting(World world, Occurrence occurrence, WindowKind window);

    /// <summary>Validates an answer without mutation and binds its payment before trigger commitment.</summary>
    /// <remarks>Cost-bearing resolvers must prepare their complete answer here; decorators forward preparation.
    /// The returned invocation body is consumed immediately by the engine.</remarks>
    Func<AbilityExecution, AbilityExecutionResult> PrepareWindow(
        World world, Occurrence occurrence, PendingAbility ability, Decision answer);

    /// <summary>Resolves an invocation admitted before its costs are paid.</summary>
    AbilityExecutionResult Resolve(World world, AbilityExecution invocation, Decision answer);
}
