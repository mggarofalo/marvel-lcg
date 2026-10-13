using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Applies and answers card-ability continuation agenda operations.</summary>
internal static class AbilityContinuationProcedure
{
    public static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.ResumeAbility:
                ActivityBatches.Append(events, world.ContinuationAbilities.ResumeAbility(world, step));
                break;
            case Steps.ResolveSpecial:
                ActivityBatches.Append(events, world.ContinuationAbilities.ResolveSpecial(
                    world, world.Cards[step.Subject], step.Seat, step.FinalStep));
                break;
            case Steps.ChooseOption:
                return world.ContinuationAbilities.Choosing(
                    world, world.Cards[step.Subject], step.Seat, step.Index, step.Tier,
                    step.FinalStep, step.EachPlayerFrame, step.FinalPlayer);
            case Steps.OrderEachPlayer:
                return EachPlayerEffects.Ordering(world, step);
            case Steps.ResolveEachPlayer:
                ActivityBatches.Append(events, EachPlayerEffects.Resolve(
                    world, world.ContinuationAbilities, step));
                break;
            default:
                throw new RulesNotImplementedException(
                    $"the ability-continuation procedure has no step '{step.What}'");
        }

        return null;
    }

    public static void Answer(
        World world, PhaseStep step, Decision input, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.ChooseOption:
                ActivityBatches.Append(events, world.ContinuationAbilities.Chose(
                    world, world.Cards[step.Subject], step.Seat, step.Index, input, step.Tier,
                    step.FinalStep, step.EachPlayerFrame, step.FinalPlayer, step.Trigger));
                break;
            case Steps.OrderEachPlayer:
                EachPlayerEffects.Ordered(world, step, input);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"continuation step '{step.What}' asked nothing and cannot take an answer");
        }
    }
}
