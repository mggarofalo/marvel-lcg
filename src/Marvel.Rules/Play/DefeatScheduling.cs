using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Keeps the rules consequence after its interrupt batch and before its captured cause.</summary>
internal static class DefeatScheduling
{
    internal static void Finalize(World world, PhaseStep? owner, Occurrence? containing,
        Card card, AgendaOperation operation, AbilityExecution prerequisite)
    {
        var continuation = new PhaseStep(operation, owner?.Round ?? 0, 8,
            Subject: card.ObjectId, Seat: card.Owner, Plan: true) { CompletionPrerequisite = prerequisite };
        if (owner is { } frame && containing is not null)
            world.Agenda.ContinueBeforeOwner(containing, frame, continuation);
        else world.Agenda.Add(continuation);
    }
}
