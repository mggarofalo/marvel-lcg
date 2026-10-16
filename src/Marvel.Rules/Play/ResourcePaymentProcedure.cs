using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Executes payment remainders on the game's ordinary agenda.</summary>
internal static class ResourcePaymentProcedure
{
    internal static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        world.Agenda.Executions.RequireContinuation(step);
        switch (step.Operation)
        {
            case ResourcePaymentOperation payment:
                payment.Payment.Resume(events);
                break;
            case CardPaymentEntryOperation entry:
                world.Agenda.Executions.Resume(entry.Entry.Execution);
                entry.Entry.Apply(world, events);
                break;
            default:
                throw new InvalidOperationException("unknown resource payment operation");
        }
        return null;
    }
}
