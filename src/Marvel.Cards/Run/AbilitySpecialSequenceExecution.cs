using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Persists a Special sequence across its complete effects and timing windows.</summary>
internal static class AbilitySpecialSequenceExecution
{
    internal static void Apply(this AbilityResolutionExecution execution,
        NextSpecialCommand command, AbilityResolutionState cast)
    {
        int round = cast.World.Agenda.Current?.Round ?? 0;
        if (command.Card is { } chosen)
        {
            var card = cast.World.Cards[chosen];
            cast.Results[AbilitySpecialSequence.Key(card)] = card.Incarnation;
        }
        // 01043a: "Resolving each ability is a step in a sequence."
        // ruling:ef19e5ae2de7d475: a new upgrade joins an unfinished sequence;
        // resolving the final Special ends it before any new upgrade joins.
        // No effect-legality filter removes an unresolved upgrade from that sequence.
        if (command.Card is { } id)
            cast.World.Agenda.Then(new PhaseStep(new AbilityContinuationAgendaOperation(AbilityContinuationStage.Special)
                { FinalSpecial = command.FinalStep && !command.Invocation }, round, 1, Subject: id, Seat: cast.Player, Plan: true));
        if (command.FinalStep) Clear(cast);
        // The parent resumes only after the owned Special procedure finishes.
        if (command.Invocation) cast.CompletedOutcome = AbilityStructuralOutcome.Full;
        var capture = execution.Capture(cast, cast.AbilityOrdinal) with
        {
            Reason = command.FinalStep ? AbilityResumeReason.EffectProcedure : AbilityResumeReason.RepeatSpecial,
        };
        var continuation = AbilityContinuations.Step(
            capture, AbilityContinuationStage.Resume, round, plan: true);
        cast.World.Agenda.Then(continuation);
        cast.ResolveEffect();
        cast.Suspend();
    }

    private static void Clear(AbilityResolutionState cast)
    {
        foreach (string key in cast.Results.Keys.Where(key =>
                     key.StartsWith(AbilitySpecialSequence.ResolvedPrefix, StringComparison.Ordinal)).ToArray())
            cast.Results.Remove(key);
    }
}
