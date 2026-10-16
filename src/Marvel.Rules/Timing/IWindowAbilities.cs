using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Discovers window abilities and executes an admitted game-owned invocation.</summary>
public interface IWindowAbilities : IAbilityDescriptions
{
    /// <summary>The abilities currently eligible in this window.</summary>
    IReadOnlyList<PendingAbility> Waiting(World world, Occurrence occurrence, WindowKind window);

    /// <summary>Resolves an invocation admitted before its costs are paid.</summary>
    AbilityExecutionResult Resolve(World world, AbilityExecution invocation, Decision answer);
}
