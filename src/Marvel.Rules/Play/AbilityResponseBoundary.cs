using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>One suspended continuation waiting for one exact owner's response window.</summary>
internal sealed record AbilityResponseBoundary(PhaseStep Owner, Occurrence Window, long Continuation,
    long? Prerequisite = null)
{
    internal void RequireFinished(Agenda agenda, PhaseStep current)
    {
        if (current.FrameId != Continuation || current.FrameGame != Owner.FrameGame
            || agenda.IsOutstanding(Owner, Window))
            throw new InvalidOperationException("The exact attack response window has not finished");
    }
}
