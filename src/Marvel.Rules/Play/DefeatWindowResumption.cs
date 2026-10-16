using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Re-reads a defeat interrupt tier after nested effects finish.</summary>
internal static class DefeatWindowResumption
{
    internal static void Resume(World world, ICardFacts facts, IWindowAbilities abilities,
        PhaseStep step, List<GameEvent> events)
    {
        DefeatAbilityOperation operation = step.Require<DefeatAbilityOperation>();
        DefeatWindow window = operation.Window;
        Occurrence origin = window.Origin
            ?? throw new InvalidOperationException("a defeat window has no occurrence");
        bool beforeDefeat = operation.Kind == DefeatAbilityKind.ResumeWouldBeDefeated;
        IReadOnlyList<PendingAbility> pending = DefeatWindowEligibility.Pending(world, abilities, step);
        if (pending.Count == 1 && AbilityTypes.IsMandatory(pending[0].Type))
        {
            ResolveMandatory(world, abilities, step, pending[0], events);
            return;
        }
        if (pending.Count > 0)
        {
            DefeatProcedure.ScheduleProcedureChoice(world, step with
            {
                Operation = operation with
                {
                    Kind = beforeDefeat ? DefeatAbilityKind.ChooseWouldBeDefeated
                        : DefeatAbilityKind.ChooseCardDefeatedAbility,
                    Window = window with { Abilities = pending, Passed = [] },
                },
                OccurrenceId = null,
            });
        }
    }

    private static void ResolveMandatory(World world, IWindowAbilities abilities,
        PhaseStep step, PendingAbility next, List<GameEvent> events)
    {
        DefeatAbilityOperation operation = step.Require<DefeatAbilityOperation>();
        Occurrence origin = operation.Window.Origin!;
        Occurrence containing = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("a defeat resume has no containing occurrence");
        origin.Trigger(WindowKind.Interrupt, next);
        if (!ReferenceEquals(containing, origin)) containing.Trigger(WindowKind.Interrupt, next);
        ActivityBatches.Append(events, abilities.ResolveWindow(world, origin, next, new Decision(next.Card, [], [])).Events);
        world.Agenda.ContinueBeforeOwner(containing, step, step with
        {
            Operation = operation with
            {
                Window = operation.Window with { Abilities = [], Passed = [] },
            },
            OccurrenceId = null,
        });
    }

}
