using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Resolves prepared Kang destination and unique-copy choices before phase responses.</summary>
internal static class KangJoinProcedure
{
    internal static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        var operation = step.Require<KangJoinOperation>();
        RequireOwner(world, operation);
        if (operation.Destination is null)
        {
            var destinations = Destinations(world, operation.Departure);
            if (destinations.Length == 0)
            {
                KangCentral.Begin(world, operation, events);
                return null;
            }
            return Describe(world, operation, "Choose a game area to join", destinations.Select(area =>
                new Affordance(area.Id, "Join_Game_Area", -1, world.FirstPlayer, $"game area {area.Id}")
                {
                    AnchorKind = AffordanceAnchorKind.Unspecified,
                    DisplayLabel = $"Join game area {area.Id}",
                    CommitLabel = $"Join game area {area.Id}",
                    Description = "Move the entire departing group into this game area after unique copies are reconciled.",
                }).ToArray());
        }
        var conflicts = ConflictingCards(world, operation);
        if (conflicts.Length > 0)
            return Describe(world, operation, "Choose a matching unique card to discard", conflicts.Select(card =>
                new Affordance(card.ObjectId, "Discard_Joined_Unique", card.ObjectId, world.FirstPlayer, card.FaceId)
                {
                    DisplayLabel = $"Discard {EffectiveCards.Title(card, world.Facts)}",
                    CommitLabel = $"Discard {EffectiveCards.Title(card, world.Facts)}",
                    Description = "Discard this copy; matching copies are checked again before the group joins.",
                }).ToArray());
        foreach (var area in operation.Departure.Group)
            world.Join(area, operation.Destination, operation.Origin.Conditions[0], events);
        world.Agenda.KangTransitions.Elimination.Join(world, operation.Departure.Source,
            operation.Destination, operation.Origin.Conditions[0], events);
        KangDepartureOrderProcedure.AfterJoin(world, operation);
        return null;
    }

    internal static void Answer(World world, PhaseStep step, Decision input, List<GameEvent> events)
    {
        var operation = step.Require<KangJoinOperation>();
        RequireOwner(world, operation);
        if (input.IsDecline || input.Targets.Count != 0 || input.Spent.Count != 0
            || input.DefinedValues.Count != 0 || input.Allocated.Count != 0)
            throw new ArgumentException("choose one offered Kang option with no extra selections");
        if (operation.Destination is null)
        {
            var destination = Destinations(world, operation.Departure).SingleOrDefault(area => area.Id == input.Affordance)
                ?? throw new ArgumentException("the selected Kang destination is not offered");
            operation = operation with { Destination = destination };
        }
        else
        {
            var card = ConflictingCards(world, operation).SingleOrDefault(card => card.ObjectId == input.Affordance)
                ?? throw new ArgumentException("the selected unique copy is not offered");
            Discard.Card(world, card, KangJoinOperation.Join, events);
        }
        // A child can share the boundary occurrence. Its answer completes this
        // exact child frame, leaving the original phase frame behind the next choice.
        world.Agenda.ContinueBeforeOwner(operation.Origin, step, step with { Operation = operation });
    }

    private static Prompt Describe(World world, KangJoinOperation operation, string question,
        IReadOnlyList<Affordance> offers) => new(world.FirstPlayer, Question.Element,
            TimingPriority.Continuous, KangJoinOperation.Join, question, false, offers)
        {
            DisplayQuestion = question,
            Description = "Resolve the departing Kang's whole-group join at the end of the phase. The first player records the agreed choice or resolves disagreement.",
            ContextCardIds = [operation.Departure.Defeated.Card],
        };

    // A departing group with players may join any remaining game area, including an
    // eliminated player's area that still holds its Kang (KangCentral.Remains). A group
    // with no players left combines straight into an area with players: our choice, since
    // its components need a player area to carry them and they would reach one at that
    // other area's own departure anyway.
    private static GameArea[] Destinations(World world, KangDeparture departure) =>
        [.. world.GameAreas.Where(area => !ReferenceEquals(area, departure.Source)
            && (departure.Group.Count > 0 ? KangCentral.Remains(world, area)
                : area.PlayAreas.Any(play => play.IsPlayers))).OrderBy(area => area.Id)];

    private static Card[] ConflictingCards(World world, KangJoinOperation operation)
    {
        var conflicts = Uniqueness.ConflictsAfterJoin(world, world.Facts, operation.Departure.Source, operation.Destination!);
        Card[] choices = [.. conflicts
            .SelectMany(pair => new[] { pair.Left, pair.Right }).Distinct()
            .Where(card => Discard.CanDiscardFromPlay(world, card)).OrderBy(card => card.ObjectId)];
        if (conflicts.Count > 0 && choices.Length == 0)
            throw new RulesNotImplementedException("Kang unique reconciliation has no discardable matching copy");
        return choices;
    }

    private static void RequireOwner(World world, KangJoinOperation operation)
    {
        if (!world.Agenda.IsOutstanding(operation.Owner, operation.Origin))
            throw new InvalidOperationException("the exact Kang phase owner is no longer outstanding");
        // Engine choice: admit the complete retained batch before any choice or
        // join commits; only AfterJoin may extend a capture for a managed join.
        KangPhaseTransitions.RequireGroups(new[] { operation.Departure }.Concat(operation.Following ?? []));
    }
}
