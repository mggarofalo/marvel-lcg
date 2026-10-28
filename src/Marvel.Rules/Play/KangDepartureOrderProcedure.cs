using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>First-player ordering of Kang's co-due local departures.</summary>
internal static class KangDepartureOrderProcedure
{
    internal static void Schedule(World world, IReadOnlyList<KangDeparture> remaining,
        PhaseStep owner, Occurrence origin) => world.Agenda.ContinueBeforeOwner(origin, owner,
            owner with { Operation = new KangDepartureOrderOperation(remaining, owner, origin) });

    internal static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        var operation = Require(world, step);
        if (operation.Remaining.Count == 1)
        {
            BeginJoin(world, operation.Remaining[0], [], operation.Owner, operation.Origin, events);
            return null;
        }
        // rr:first-player.3: "If two or more effects would resolve simultaneously,
        // the first player decides the order in which to resolve them."
        // Physical-id offer order is an engine bookkeeping choice, never a selection.
        return new Prompt(world.FirstPlayer, Question.Element, TimingPriority.Continuous,
            KangDepartureOrderOperation.Order, "Choose the next Kang departure to resolve", false,
            operation.Remaining.OrderBy(item => item.Defeated.Card).Select(item =>
                new Affordance(item.Defeated.Card, "Resolve_Kang_Departure", item.Defeated.Card,
                    world.FirstPlayer, world.Cards[item.Defeated.Card].FaceId)
                {
                    DisplayLabel = $"Resolve departure from game area {item.Source.Id}",
                    CommitLabel = $"Resolve game area {item.Source.Id} next",
                    Description = "Resolve this local departure completely, including its whole-group join, before the other departures.",
                }).ToArray())
        {
            DisplayQuestion = "Choose the next Kang departure to resolve",
            Description = "These local effects are due at the same phase ending. The first player chooses their resolution order; the joining destination is chosen next.",
            ContextCardIds = operation.Remaining.Select(item => item.Defeated.Card).ToArray(),
        };
    }

    internal static void Answer(World world, PhaseStep step, Decision input, List<GameEvent> events)
    {
        var operation = Require(world, step);
        if (input.IsDecline || input.Targets.Count != 0 || input.Spent.Count != 0
            || input.DefinedValues.Count != 0 || input.Allocated.Count != 0)
            throw new ArgumentException("choose one offered Kang departure with no extra selections");
        var selected = operation.Remaining.SingleOrDefault(item => item.Defeated.Card == input.Affordance)
            ?? throw new ArgumentException("the selected Kang departure is not offered");
        BeginJoin(world, selected, operation.Remaining.Where(item => !ReferenceEquals(item, selected)).ToArray(),
            operation.Owner, operation.Origin, events);
    }

    internal static void BeginJoin(World world, KangDeparture departure, IReadOnlyList<KangDeparture> following,
        PhaseStep owner, Occurrence origin, List<GameEvent> events)
    {
        KangPhaseTransitions.PreflightDeparture(world, departure);
        if (departure.CompletedScheme is { } scheme)
        {
            KangFirstSplit.Remove(world, world.Cards[departure.Defeated.Card], "Kang local completion", events);
            KangFirstSplit.Remove(world, world.Cards[scheme.Card], "Kang local completion", events);
        }
        world.Agenda.ContinueBeforeOwner(origin, owner, owner with
        {
            Operation = new KangJoinOperation(departure, owner, origin, Following: following),
            Subject = departure.Defeated.Card,
        });
    }

    internal static void AfterJoin(World world, KangJoinOperation operation)
    {
        if (operation.Following is not { Count: > 0 } following) return;
        // MC11 moves the whole combined group. Only this committed join extends
        // a captured group; its original effect/card incarnation remains unchanged.
        var remaining = following.Select(item => ReferenceEquals(item.Source, operation.Destination)
            ? item with { Group = Array.AsReadOnly(item.Group.Concat(operation.Departure.Group)
                .OrderBy(area => area.Player).ToArray()) } : item).ToArray();
        Schedule(world, remaining, operation.Owner, operation.Origin);
    }

    private static KangDepartureOrderOperation Require(World world, PhaseStep step)
    {
        var operation = step.Require<KangDepartureOrderOperation>();
        if (!world.Agenda.IsOutstanding(operation.Owner, operation.Origin))
            throw new InvalidOperationException("the exact Kang phase owner is no longer outstanding");
        foreach (var departure in operation.Remaining) KangPhaseTransitions.RequireGroup(departure);
        return operation;
    }
}
