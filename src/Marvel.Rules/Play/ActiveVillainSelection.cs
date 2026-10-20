using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Resolves the active counter using live side-scheme threat and first-player ties.</summary>
public static class ActiveVillainSelection
{
    /// <summary>Moves the counter immediately, or schedules the first player's required tie choice.</summary>
    public static bool Schedule(World world, string trigger, List<GameEvent> events)
    {
        var candidates = Candidates(world);
        if (candidates.Count == 0)
            throw new RulesNotImplementedException("active villain selection has no living villain");
        if (candidates.Count == 1)
        {
            world.EncounterTopology!.SelectActive(candidates[0], trigger, events);
            return false;
        }
        var step = new PhaseStep(new ActiveVillainSelectionOperation(trigger),
            world.Agenda.Current?.Round ?? 0, 0, Seat: world.FirstPlayer, Plan: true);
        DefeatProcedure.ScheduleProcedureChoice(world, step);
        return true;
    }

    internal static Prompt Describe(World world) =>
        new(world.FirstPlayer, Question.Element, TimingPriority.Untimed,
            "ChooseActiveVillain", "Choose the active villain", false,
            Candidates(world).Select(card => new Affordance(card.ObjectId,
                "Choose_Active_Villain", card.ObjectId, world.FirstPlayer, card.FaceId)
            {
                DisplayLabel = $"Make {EffectiveCards.Title(card, world.Facts)} active",
                CommitLabel = $"Make {EffectiveCards.Title(card, world.Facts)} active",
                Description = "Move the active counter to this villain.",
            }).ToList())
        {
            PublicKind = PublicDecisionKind.Choice,
            DisplayQuestion = "Choose the active villain",
            Description = "These villains' side schemes are tied for the most threat. The first player decides.",
            ContextCardIds = Candidates(world).Select(card => card.ObjectId).ToArray(),
        };

    internal static void Answer(World world, PhaseStep step, Decision input, List<GameEvent> events)
    {
        var selected = Candidates(world).SingleOrDefault(card => card.ObjectId == input.Affordance);
        if (selected is null || input.Targets.Count != 0 || input.Spent.Count != 0
            || input.DefinedValues.Count != 0 || input.Allocated.Count != 0)
            throw new ArgumentException("choose exactly one currently tied villain with no extra selections");
        world.EncounterTopology!.SelectActive(selected,
            step.Require<ActiveVillainSelectionOperation>().Trigger, events);
    }

    private static List<Card> Candidates(World world)
    {
        // pack:mc03:new-rules: move to the villain whose scheme has most threat;
        // "In case of a tie, the first player decides." Setup order breaks no ties.
        return [.. EncounterSchemeRelations.AtThreatExtreme(world, maximum: true)];
    }
}
