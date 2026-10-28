using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Starts ordered authored abilities only after their predecessor's owned work completes.</summary>
internal static class AbilityBatchExecution
{
    internal static void RunOrdered(this AbilityResolutionExecution execution,
        IEnumerable<CompiledCardAbility> abilities, Func<CompiledCardAbility, AbilityResolutionState> create)
    {
        var ordered = abilities.ToArray();
        RequireStandaloneDestinations(ordered);
        bool pending = false;
        PhaseStep? owner = null;
        Occurrence? containing = null;
        bool started = false;
        foreach (var ability in ordered)
        {
            var cast = create(ability);
            if (!started)
            {
                owner = cast.World.Agenda.Current;
                containing = cast.World.Agenda.Occurrence;
                started = true;
            }
            int ordinal = execution.AbilitiesOn(cast.Source, cast.AbilityFace)
                .Where(candidate => candidate.Trigger.Timing == ability.Trigger.Timing)
                .ToList().IndexOf(ability);
            cast.RestoreAbility(ordinal, []);
            if (pending)
            {
                Schedule(execution, cast, owner, containing);
                continue;
            }
            cast.TrackResolution(ordinal);
            execution.Run(ability, cast);
            cast.CompleteResolution();
            pending = cast.Execution is { IsComplete: false };
        }
    }

    internal static void RequireStandaloneDestinations(IEnumerable<CompiledCardAbility> abilities)
    {
        foreach (var ability in abilities)
            AbilityFormDestinationResolver.RequireStandalone(ability.Effect);
    }

    private static void Schedule(AbilityResolutionExecution execution, AbilityResolutionState cast,
        PhaseStep? owner, Occurrence? containing)
    {
        var continuation = AbilityContinuations.Step(
            execution.Capture(cast, cast.AbilityOrdinal) with { Reason = AbilityResumeReason.StartAbility },
            AbilityContinuationStage.Resume, owner?.Round ?? 0);
        if (owner is { } frame && containing is not null)
            cast.World.Agenda.ContinueBeforeOwner(containing, frame, continuation, cast.Occurrence);
        else cast.World.Agenda.Add(continuation);
    }
}
