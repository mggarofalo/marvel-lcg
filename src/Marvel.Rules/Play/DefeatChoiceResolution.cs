using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Answers one mandatory ordering or optional opportunity in a defeat window.</summary>
internal static class DefeatChoiceResolution
{
    internal static void Resolve(World world, ICardFacts facts, IWindowAbilities abilities,
        PhaseStep step, Decision input, List<GameEvent> events)
    {
        if (input.IsDecline) Decline(world, facts, abilities, step, events);
        else Choose(world, abilities, step, input, events);
    }

    private static void Decline(World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step, List<GameEvent> events)
    {
        DefeatAbilityOperation operation = step.Require<DefeatAbilityOperation>();
        IReadOnlyList<PendingAbility> pending = DefeatWindowEligibility.Pending(world, abilities, step);
        if (pending.All(ability => AbilityTypes.IsMandatory(ability.Type)))
            throw new RulesNotImplementedException("a forced defeat ability ordering cannot be declined");
        int player = Player(world, step, pending, mandatory: false);
        int[] passed = [.. (operation.Window.Passed ?? []).Append(player).Distinct()];
        if (world.PlayerOrder.Any(seat => !passed.Contains(seat)
            && pending.Any(ability => ability.Player < 0 || ability.Player == seat)))
        {
            DefeatProcedure.ScheduleProcedureChoice(world, step with
            {
                Operation = operation with { Window = operation.Window with { Passed = passed } },
                OccurrenceId = null,
            });
        }
    }

    private static void Choose(World world, IWindowAbilities abilities, PhaseStep step,
        Decision input, List<GameEvent> events)
    {
        DefeatAbilityOperation operation = step.Require<DefeatAbilityOperation>();
        Occurrence occurrence = operation.Window.Origin
            ?? throw new InvalidOperationException("a defeat choice has no occurrence");
        IReadOnlyList<PendingAbility> pending = DefeatWindowEligibility.Pending(world, abilities, step);
        bool mandatory = pending.All(ability => AbilityTypes.IsMandatory(ability.Type));
        int player = Player(world, step, pending, mandatory);
        var offered = mandatory ? pending
            : pending.Where(ability => ability.Player < 0 || ability.Player == player).ToList();
        PendingAbility chosen = offered
            .Where(ability => abilities.Describe(world, ability).Id == input.Affordance)
            .Select(ability => (PendingAbility?)ability).SingleOrDefault()
            ?? throw new RulesNotImplementedException("the selected defeat ability was not offered");
        occurrence = DefeatWindowEligibility.Origin(world, abilities, step, chosen);
        Occurrence containing = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("a defeat choice has no containing occurrence");
        ActivityBatches.Append(events, abilities.ResolveWindow(world, occurrence, chosen, input, () =>
        {
            occurrence.Trigger(WindowKind.Interrupt, chosen);
            if (!ReferenceEquals(containing, occurrence))
                containing.Trigger(WindowKind.Interrupt, chosen);
        }).Events);
        world.Agenda.ContinueBeforeOwner(containing, step, step with
        {
            Operation = operation with
            {
                Kind = operation.Kind == DefeatAbilityKind.ChooseWouldBeDefeated
                    ? DefeatAbilityKind.ResumeWouldBeDefeated : DefeatAbilityKind.ResumeCardDefeatedAbility,
                Window = operation.Window with
                {
                    Abilities = [.. pending.Where(ability => ability != chosen)],
                    Passed = [],
                },
            },
            OccurrenceId = null,
        });
    }

    internal static int Player(World world, PhaseStep step,
        IReadOnlyList<PendingAbility> pending, bool mandatory)
    {
        if (mandatory) return world.FirstPlayer;
        IReadOnlyList<int> passed = step.Require<DefeatAbilityOperation>().Window.Passed ?? [];
        return world.PlayerOrder.FirstOrDefault(player => !passed.Contains(player)
            && pending.Any(ability => ability.Player < 0 || ability.Player == player),
            step.Seat >= 0 ? step.Seat : world.FirstPlayer);
    }
}
