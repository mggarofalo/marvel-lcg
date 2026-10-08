using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Offers the next currently engaged minion after the preceding activation settles.</summary>
internal static class MinionActivationChoice
{
    internal static List<Card> Remaining(World world, int seat, PhaseStep step) => world
        .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat)).Cards
        .Where(minion => !(step.ActivatedEnemies ?? []).Contains(minion.ObjectId)
            || step.ProcedureAmounts?.TryGetValue(minion.ObjectId, out long incarnation) == true
                && incarnation != minion.Incarnation)
        .OrderBy(minion => minion.ObjectId).ToList();

    internal static Prompt Describe(World world, Card villain, int seat, IReadOnlyList<Card> remaining) =>
        new(seat, Question.Order, TimingPriority.Untimed, Steps.EnemiesActivate,
            $"{world.Seats[seat].Name} chooses the next engaged minion", false,
            remaining.Select(card => new Affordance(card.ObjectId, "Activate_Next",
                card.ObjectId, seat, EffectiveCards.FaceId(card))
            {
                DisplayLabel = $"Activate {EffectiveCards.Title(card, world.Facts)} next",
                CommitLabel = $"Activate {EffectiveCards.Title(card, world.Facts)} next",
                Description = "Resolve this minion's activation completely, then choose the next remaining minion.",
            }).ToList())
        {
            PublicKind = PublicDecisionKind.MinionActivationOrder,
            DisplayQuestion = "Choose the next minion to activate",
            ContextCardIds = [.. remaining.Select(card => card.ObjectId)],
            // rr:minion.3: "one minion at a time and in an order of the engaged
            // player's choosing"; rr:activation.1 reads form at each activation.
            Description = $"{EffectiveCards.Title(villain, world.Facts)} has finished activating against "
                + $"{world.Seats[seat].Name}. Choose one engaged minion to activate next. "
                + "Each attacks in hero form or schemes in alter-ego form. "
                + "Finish that activation and its triggered abilities before choosing again.",
        };

    internal static void Answer(World world, PhaseStep step, Decision input)
    {
        IReadOnlyList<int> players = step.ActivationPlayers ?? world.PlayerOrder.ToList();
        if (step.Index < 0 || step.Index >= players.Count)
            throw new RulesNotImplementedException("the minion choice has no engaged player");
        List<Card> candidates = Remaining(world, players[step.Index], step);
        if (input.IsDecline || input.Targets.Count != 0
            || candidates.All(card => card.ObjectId != input.Affordance))
            throw new RulesNotImplementedException("choose exactly one currently engaged minion to activate next");
        // The single chosen id is engine continuation data, consumed before the
        // next choice. rr:minion.4 makes newly engaged minions join that next read.
        world.Agenda.Then(step with
        {
            ActivationOrder = [input.Affordance],
            ProcedureCandidates = [.. candidates.Select(card => card.ObjectId)],
            OccurrenceId = null,
        });
    }
}
