using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A read-only projection of a forced would-be-defeated interrupt.</summary>

internal static class PhaseTransitionProcedure
{
    internal static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.PassFirstPlayerToken:
                PassFirstPlayerToken(world);
                break;
            case Steps.EndVillainPhase:
                PhaseEnd.EndVillainPhase(world, facts, events);
                break;
            case Steps.DrawToHandSize:
                PhaseEnd.DrawToHandSize(world, facts, events);
                break;
            case Steps.ReadyCards:
                PhaseEnd.ReadyCards(world, events);
                break;
            case Steps.EndPlayerPhase:
                PhaseEnd.EndPlayerPhase(world, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"the phase-transition procedure has no step '{step.What}'");
        }
        return null;
    }

    /// <summary>Step 5. <c>rr:villain-phase.step.5</c>, to the next clockwise player.</summary>
    internal static void PassFirstPlayerToken(World world) =>
        world.FirstPlayer = world.Players > 0 ? (world.FirstPlayer + 1) % world.Players : 0;
}
