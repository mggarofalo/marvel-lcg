using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    private bool TryRunCardMovement(AbilityEffect instruction, AbilityResolutionState cast)
    {
        var result = AbilityDeckAndRevealExecution.Run(instruction,
            new AbilityDeckAndRevealContext(
                cast.ExpressionContext(), cast.Trigger, cast.Events,
                cardPlayAbilities, readinessAbilities,
                [.. cast.Discarded]));
        if (!result.IsHandled)
        {
            return false;
        }
        foreach (var (key, value) in result.Values)
            cast.Results[key] = value;
        if (result.Reveal is { } reveal)
            ScheduleReveal(reveal, cast.World);
        if (result.ResolveEffect)
            cast.ResolveEffect();
        return true;
    }

    private static void ScheduleReveal(AbilityRevealRequest reveal, World world) =>
        world.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCard,
            world.Agenda.Current?.Round ?? 0,
            4,
            Index: reveal.Player,
            Subject: reveal.Card,
            Seat: reveal.Player));
}
