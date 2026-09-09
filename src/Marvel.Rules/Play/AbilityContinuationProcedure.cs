using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Applies and answers card-ability continuation agenda operations.</summary>
internal static class AbilityContinuationProcedure
{
    public static bool Handles(PhaseStep step) => step.What is
        Steps.ResumeAbility or Steps.ResolveSpecial or Steps.ChooseOption
        or Steps.OrderEachPlayer or Steps.ResolveEachPlayer;

    public static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.ResumeAbility:
                events.AddRange(world.ContinuationAbilities.ResumeAbility(world, step));
                break;
            case Steps.ResolveSpecial:
                events.AddRange(world.ContinuationAbilities.ResolveSpecial(
                    world, world.Cards[step.Subject], step.Seat, step.FinalStep));
                break;
            case Steps.ChooseOption:
                return world.ContinuationAbilities.Choosing(
                    world, world.Cards[step.Subject], step.Seat, step.Index, step.Tier,
                    step.FinalStep, step.EachPlayerFrame, step.FinalPlayer);
            case Steps.OrderEachPlayer:
                return EachPlayerEffects.Ordering(world, step);
            case Steps.ResolveEachPlayer:
                events.AddRange(EachPlayerEffects.Resolve(
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
                events.AddRange(world.ContinuationAbilities.Chose(
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
