using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionCardMovement
{
    internal static bool TryRunCardMovement(this AbilityResolutionExecution execution, AbilityEffect instruction, AbilityResolutionState cast)
    {
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
            execution.ScheduleReveal(reveal, cast.World);
        if (result.ResolveEffect)
            cast.ResolveEffect();
        return true;
    }

    internal static void ScheduleReveal(this AbilityResolutionExecution execution, AbilityRevealRequest reveal, World world) =>
        world.Agenda.Then(new PhaseStep(
            new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            world.Agenda.Current?.Round ?? 0,
            4,
            Index: reveal.Player,
            Subject: reveal.Card,
            Seat: reveal.Player));
}
