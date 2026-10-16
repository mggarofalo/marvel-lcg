using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Admits an Action invocation before its resolver can pay costs or create child work.</summary>
public static class ActionResolution
{
    /// <summary>Execute accepted Action inputs under one checked invocation lifecycle.</summary>
    public static AbilityExecutionResult ExecuteAction(this ICardActionAbilities abilities,
        World world, PlayerAction action, Occurrence? occurrence = null)
    {
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(action);
        var offer = abilities.Describe(world, action.Ability);
        var answer = new Decision(offer.Id, action.Chosen, action.Paying, action.Values, action.Allocations);
        // A component caller can initiate outside a live player-turn frame. Its
        // standalone occurrence still owns all costs, continuations and cleanup.
        occurrence ??= new Occurrence(0, [Steps.TurnAction],
            Subject: action.Ability.Card, Player: action.Ability.Player);
        return world.Agenda.Executions.Run(occurrence, action.Ability,
            invocation => abilities.Act(world, invocation, answer));
    }
}
