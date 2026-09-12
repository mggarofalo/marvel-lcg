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
                [.. cast.Discarded]));
        if (!result.IsHandled)
        {
            return false;
        }
        foreach (var (key, value) in result.Values)
            cast.Results[key] = value;
        if (result.Reveal is { } reveal)
            execution.ScheduleReveal(reveal, cast.World);
        if (result.ResolveEffect)
            cast.ResolveEffect();
        return true;
    }

    internal static void ScheduleReveal(this AbilityResolutionExecution execution, AbilityRevealRequest reveal, World world) =>
        world.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCard,
            world.Agenda.Current?.Round ?? 0,
            4,
            Index: reveal.Player,
            Subject: reveal.Card,
            Seat: reveal.Player));
}
