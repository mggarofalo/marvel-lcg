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

    /// <summary>Step 5. <c>rr:villain-phase.step.5</c>, to the next participating player.</summary>
    internal static void PassFirstPlayerToken(World world)
    {
        if (world.Players == 0)
        {
            world.FirstPlayer = 0;
            return;
        }

        for (int offset = 1; offset <= world.Players; offset++)
        {
            int player = (world.FirstPlayer + offset) % world.Players;
            if (!world.Seats[player].Eliminated)
            {
                // `rr:villain-phase.step.5`: "Pass the first player token to
                // the next clockwise player." `rr:in-player-order.2`: "The
                // phrase 'next player' always refers to the next (clockwise)
                // player in player order." `rr:player-elimination.6`: "Effects
                // that refer to the players in the game ignore eliminated players."
                world.FirstPlayer = player;
                return;
            }
        }
    }
}
