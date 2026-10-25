using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionCardMovement
{
    internal static bool TryRunCardMovement(this AbilityResolutionExecution execution, AbilityEffect instruction, AbilityResolutionState cast)
    {
        if (AbilityAllyEntry.TryRun(execution, instruction, cast)) return true;
        var result = AbilityDeckAndRevealExecution.Run(instruction,
            new AbilityDeckAndRevealContext(
                cast.ExpressionContext(), cast.Trigger, cast.Events,
                execution.cardPlayAbilities, execution.readinessAbilities,
                [.. cast.Discarded]) { Program = execution.program });
        if (!result.IsHandled)
        {
            return false;
        }
        foreach (var (key, value) in result.Values)
            cast.Results[key] = value;
        if (result.Entry is { } entry)
        {
            var completion = new EffectCompletion(1, 1);
            cast.PendingCompletion = completion;
            var capture = AbilityContinuationScheduling.ForEffectProcedure(
                execution.Capture(cast, execution.AbilityOrdinal(instruction, cast)));
            var continuation = AbilityContinuations.Step(capture,
                AbilityContinuationStage.Resume, cast.World.Agenda.Current?.Round ?? 0, plan: true);
            EncounterEntry.Schedule(cast.World, entry, completion, continuation);
            cast.Suspend();
        }
        if (result.Reveal is { } reveal)
            execution.ScheduleReveal(instruction, reveal, cast);
        if (result.ResolveEffect)
            cast.ResolveEffect();
        return true;
    }

    private static void ScheduleReveal(this AbilityResolutionExecution execution,
        AbilityEffect instruction, AbilityRevealRequest reveal, AbilityResolutionState cast)
    {
        // rr:reveal.step.4: "this same resolution procedure applies" to card-text reveals.
        // The child resolves completely before the containing ability advances.
        // The receipt concerns revealing the card, even if its own effects are canceled.
        cast.CompletedOutcome = AbilityStructuralOutcome.Full;
        var capture = AbilityContinuationScheduling.ForEffectProcedure(
            execution.Capture(cast, execution.AbilityOrdinal(instruction, cast)));
        int round = cast.World.Agenda.Current?.Round ?? 0;
        var child = new PhaseStep(
            new RevealStageOperation(RevealStageKind.RevealEncounterCard), round, 4,
            Index: reveal.Player, Subject: reveal.Card, Seat: reveal.Player);
        var continuation = AbilityContinuations.Step(capture, AbilityContinuationStage.Resume, round, plan: true);
        if (cast.World.Agenda.Current is not null) cast.World.Agenda.Now([child, continuation]);
        else
        {
            cast.World.Agenda.Add(child);
            cast.World.Agenda.Add(continuation);
        }
        cast.Suspend();
    }
}
