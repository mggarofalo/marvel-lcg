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
        IReadOnlyList<PendingAbility> carried = window.Abilities ?? [];
        if (carried.Count == 1 && AbilityTypes.IsMandatory(carried[0].Type))
        {
            ResolveMandatory(world, abilities, step, carried[0], events);
            return;
        }
        bool beforeDefeat = operation.Kind == DefeatAbilityKind.ResumeWouldBeDefeated;
        IReadOnlyList<PendingAbility> pending = carried.Count > 0 ? carried
            : FirstInterruptTier(abilities, world, origin,
                excludeCard: beforeDefeat ? -1 : step.Subject, after: window.Tier);
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
        else if (beforeDefeat) DefeatProcedure.FinishWouldBeDefeated(world, facts, step, events);
        else DefeatProcedure.FinalizeCardDefeat(world, facts, step, events);
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
        ActivityBatches.Append(events, abilities.Resolve(world, origin, next, [], []));
        world.Agenda.ContinueBeforeOwner(containing, step, step with
        {
            Operation = operation with
            {
                Window = operation.Window with { Abilities = [], Passed = [], Tier = next.Type },
            },
            OccurrenceId = null,
        });
    }

    private static IReadOnlyList<PendingAbility> FirstInterruptTier(
        IWindowAbilities abilities, World world, Occurrence occurrence,
        int excludeCard, AbilityType? after)
    {
        var tiers = AbilityWindow.Tiers(
            abilities.Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(ability => ability.Card != excludeCard),
            WindowKind.Interrupt, occurrence);
        var tier = tiers.FirstOrDefault(candidate => after is null
            || candidate.Priority > AbilityTypes.PriorityOf(after.Value));
        if (tier.Abilities is null) return [];
        var (forced, optional) = AbilityWindow.Split(tier);
        return forced.Count > 0 ? [.. forced] : [.. optional];
    }
}
