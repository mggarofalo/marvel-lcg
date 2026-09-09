using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Applies an accepted player Action stored on the agenda.</summary>
internal static class PlayerActionProcedure
{
    public static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        if (step.What == Steps.ChooseAllyForLimit)
        {
            return PlayerLimitProcedure.ChooseAlly(world, facts, step.Seat);
        }
        if (step.What == Steps.ChooseRestrictedCard)
        {
            return PlayerLimitProcedure.ChooseRestricted(world, facts, step.Seat);
        }
        if (step.What != Steps.TurnAction)
        {
            throw new RulesNotImplementedException(
                $"the player-action procedure has no step '{step.What}'");
        }
        if (step.PlayerAction is not { } action)
        {
            throw new InvalidOperationException(
                "a player Action agenda step has no accepted action");
        }

        var occurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException(
                "an applying player Action has no occurrence");
        try
        {
            events.AddRange(world.ActionAbilities.Act(
                world, action.Ability, action.Paying, action.Chosen, occurrence,
                action.DefinedValues, action.Allocated));
        }
        catch
        {
            // A refused command must not become permanent agenda work. These
            // command failure semantics are an engine choice: the open turn
            // prompt remains retryable.
            world.Agenda.Cancel(occurrence);
            throw;
        }

        // Children share the Action occurrence and resolve before responses.
        world.Agenda.Advance(step, occurrence);
        world.Agenda.BeforeResponses(occurrence);
        return null;
    }

    public static void Answer(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.ChooseAllyForLimit:
                PlayerLimitProcedure.DiscardAlly(
                    world, facts, step.Seat, input, events);
                break;
            case Steps.ChooseRestrictedCard:
                PlayerLimitProcedure.DiscardRestricted(
                    world, facts, step, input, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"player-action step '{step.What}' asked nothing and cannot take an answer");
        }
    }
}
