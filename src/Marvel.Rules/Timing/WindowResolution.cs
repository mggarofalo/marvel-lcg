using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Admits window execution before a resolver can pay costs or create child work.</summary>
public static class WindowResolution
{
    /// <summary>Run one invocation and validate its explicit completion or suspension.</summary>
    public static AbilityExecutionResult ResolveWindow(this IWindowAbilities abilities,
        World world, Occurrence occurrence, PendingAbility ability, Decision answer, Action? accepted = null)
    {
        var prepared = abilities.PrepareWindow(world, occurrence, ability, answer);
        // Engine choice: only a fully prepared answer consumes its timing opportunity.
        // Accepted costs and effects may suspend; their later failures are not rolled back.
        accepted?.Invoke();
        return world.Agenda.Executions.Run(occurrence, ability, prepared);
    }
}
