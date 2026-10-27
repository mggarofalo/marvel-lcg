using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Observes phase beginnings when step one is reached, after preceding phase-end work.</summary>
public static class VillainPhaseBeginning
{
    /// <summary>The villain phase begins before step one places threat.</summary>
    public const string Begins = "WhenVillainPhaseBegins";

    internal static bool Observe(World world, IWindowAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        if (step.Operation is not ThreatStageOperation { Kind: ThreatStageKind.PlaceThreat, } threat
            || world.Agenda.Occurrence is not null) return false;

        if (!threat.PhaseBegun)
        {
            // The existing threat frame records the beginning across suspension.
            Mark(world, step, threat with { PhaseBegun = true });
            events.Add(new PhaseStarted("Villain") { Round = step.Round, Trigger = Begins, Verb = "Phase_Started" });
            // Unobserved phases retain their original frame and occurrence identities.
            if (abilities.ObservesVillainPhaseBeginning(world))
            {
                // Index -1 is an engine coordinate reserved ahead of step one.
                world.Agenda.Now(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.BeginVillainPhase),
                    step.Round, step.Number, Index: -1));
                return true;
            }
        }
        if (!threat.CompletionObserved)
        {
            // Beginning windows may introduce observers before step one is reached.
            Mark(world, step, threat with { PhaseBegun = true, CompletionObserved = true });
            if (abilities.ObservesVillainStepOneCompletion(world))
                world.Agenda.Then(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainStepOne),
                    step.Round, step.Number, Index: step.Index + 1));
        }
        return false;
    }

    private static void Mark(World world, PhaseStep step, ThreatStageOperation threat)
    {
        var frame = world.Agenda.items[0];
        world.Agenda.items[0] = (step with { Operation = threat }, frame.Stage, frame.Occurrence);
    }
}
