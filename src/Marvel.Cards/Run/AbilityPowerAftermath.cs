using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Retains the containing ability until the attack's complete response window ends.</summary>
internal sealed class AbilityPowerAftermath(AbilityResolutionState initial, string power)
{
    private bool scheduled;

    internal bool Complete(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        if (power != BasicPowers.AttackVerb || scheduled) return true;
        var state = initial.PowerExecution
            ?? throw new InvalidOperationException("The attack has no retained execution");
        // rr:event: "then place the event card in its owner's discard pile."
        // Structural selection wrappers do not leave another effect to resolve.
        if (state.Decoded.Frames.All(frame => frame is ChoiceFrame or ChoiceOtherwiseFrame
            or ConditionalFrame or DefenseFrame
            || frame is SequenceFrame sequence && sequence.Next >= sequence.Count)) return true;
        var owner = state.WindowOwner
            ?? throw new InvalidOperationException("The attack has no response-window owner");
        var occurrence = state.Occurrence
            ?? throw new InvalidOperationException("The attack has no response occurrence");
        scheduled = true;
        var capture = execution.Capture(cast, state.Decoded.Ordinal) with
        { Reason = AbilityResumeReason.PowerAftermath };
        var continuation = AbilityContinuations.Step(capture,
            AbilityContinuationStage.Resume, owner.Round, plan: true);
        cast.World.Agenda.AfterResponseWindow(cast.Execution!, owner, occurrence, continuation);
        cast.Suspend();
        cast.CompleteResolution();
        return false;
    }
}
