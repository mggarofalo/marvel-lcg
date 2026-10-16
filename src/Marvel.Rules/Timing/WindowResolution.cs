using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Admits window execution before a resolver can pay costs or create child work.</summary>
public static class WindowResolution
{
    /// <summary>Run one invocation and validate its explicit completion or suspension.</summary>
    public static AbilityExecutionResult ResolveWindow(this IWindowAbilities abilities,
        World world, Occurrence occurrence, PendingAbility ability, Decision answer) =>
        world.Agenda.Executions.Run(occurrence, ability,
            invocation => abilities.Resolve(world, invocation, answer));
}
