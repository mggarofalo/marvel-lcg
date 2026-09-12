using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A read-only projection of a forced would-be-defeated interrupt.</summary>

internal static class ActivationProcedure
{
    internal static Prompt? Apply(World world, ICardFacts facts, PhaseStep step) =>
        step.What == Steps.EnemiesActivate
            ? Plan(world, facts, step)
            : throw new RulesNotImplementedException(
                $"the activation procedure has no step '{step.What}'");

    internal static void Answer(World world, PhaseStep step, Decision input)
    {
        if (step.What != Steps.EnemiesActivate)
        {
            throw new RulesNotImplementedException(
                $"activation step '{step.What}' asked nothing and cannot take an answer");
        }
        Order(world, step, input);
    }

    /// <summary>
    /// Step 2, one enemy at a time — <c>rr:villain-phase.step.2</c>, "in player
    /// order, each player resolves".
    /// </summary>
    internal static Prompt? Plan(World world, ICardFacts facts, PhaseStep step)
    {
        var playerOrder = step.ActivationPlayers ?? world.PlayerOrder.ToList();
        if (step.Index >= playerOrder.Count)
        {
            return null;
        }

        var villain = world.TheCardIn(DeckType.VillainArea);
        if (villain is null)
        {
            return null;
        }

        int seat = playerOrder[step.Index];
        var activated = step.ActivatedEnemies ?? [];

        // An eliminated seat remains in this procedure's stable order so its
        // removal cannot shift the next player under the current index. It has
        // no enemies left to activate; advance and clear the per-player set.
        if (world.Seats[seat].Eliminated)
        {
            world.Agenda.Then(step with
            {
                Index = step.Index + 1,
                ActivatedEnemies = [],
                ActivationPlayers = playerOrder,
                OccurrenceId = null,
            });
            return null;
        }

        // `rr:activation.1`: hero form and the enemy attacks, alter-ego form
        // and it schemes. Read the form immediately before each activation:
        // an earlier activation can change it.
        var identity = world.Seats[seat].IdentityCard;
        bool attacking = facts.Kind(identity.FaceId) != CardKind.AlterEgo;

        var remaining = RemainingMinions(world, seat, activated);
        if (NeedsOrder(step, villain, activated, remaining))
            return OrderPrompt(world, villain, seat, remaining);

        int? enemy = NextEnemy(step, villain, activated, remaining);

        if (enemy is { } next)
        {
            world.Agenda.Then(new PhaseStep(
                attacking ? Steps.Attack : Steps.Scheme,
                step.Round, 2, Index: seat, Subject: next, Seat: seat));
            world.Agenda.Then(step with
            {
                ActivatedEnemies = [.. activated, next],
                ActivationPlayers = playerOrder,
                ActivationOrder = step.ActivationOrder,
                OccurrenceId = null,
            });
            return null;
        }

        // `rr:minion.4`: a minion that becomes engaged while engaged minions
        // are activating joins this procedure. The continuation above therefore
        // re-reads the area only after the preceding activation has completely
        // resolved. The list prevents a surviving minion from being chosen
        // again. A newly engaged group is ordered when this chosen order has
        // been exhausted.
        world.Agenda.Then(step with
        {
            Index = step.Index + 1,
            ActivatedEnemies = [],
            ActivationPlayers = playerOrder,
            ActivationOrder = null,
            OccurrenceId = null,
        });
        return null;
    }

    private static List<Card> RemainingMinions(
        World world, int seat, IReadOnlyList<int> activated) => world
        .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat)).Cards
        .Where(minion => !activated.Contains(minion.ObjectId))
        .OrderBy(minion => minion.ObjectId).ToList();

    private static bool NeedsOrder(
        PhaseStep step, Card villain, IReadOnlyList<int> activated, List<Card> remaining)
    {
        bool orderHasCandidate = step.ActivationOrder?.Any(candidate =>
            !activated.Contains(candidate)
            && remaining.Any(minion => minion.ObjectId == candidate)) == true;
        return activated.Contains(villain.ObjectId) && !orderHasCandidate && remaining.Count > 1;
    }

    private static Prompt OrderPrompt(World world, Card villain, int seat, List<Card> remaining)
    {
        var ids = remaining.Select(minion => minion.ObjectId).ToList();
        return new Prompt(seat, Question.Order, TimingPriority.Untimed,
            Steps.EnemiesActivate,
            $"{world.Seats[seat].Name} orders engaged minion activations", false,
            [new Affordance(villain.ObjectId, "Order", villain.ObjectId, seat,
                "engaged minions", new TargetRequest(ids, ids.Count, ids.Count,
                    Rule: "rr:minion.3"))]);
    }

    private static int? NextEnemy(
        PhaseStep step, Card villain, IReadOnlyList<int> activated, List<Card> remaining)
    {
        if (!activated.Contains(villain.ObjectId)) return villain.ObjectId;
        int? ordered = step.ActivationOrder?
            .Where(candidate => !activated.Contains(candidate)
                && remaining.Any(minion => minion.ObjectId == candidate))
            .Select(candidate => (int?)candidate).FirstOrDefault();
        return ordered ?? remaining.Select(minion => (int?)minion.ObjectId).FirstOrDefault();
    }

    internal static void Order(World world, PhaseStep step, Decision input)
    {
        var playerOrder = step.ActivationPlayers ?? world.PlayerOrder.ToList();
        if (step.Index < 0 || step.Index >= playerOrder.Count)
        {
            throw new RulesNotImplementedException(
                "the minion-order continuation has no engaged player");
        }
        int seat = playerOrder[step.Index];
        var villain = world.TheCardIn(DeckType.VillainArea)
            ?? throw new RulesNotImplementedException(
                "minion activations cannot be ordered without a villain");
        var activated = step.ActivatedEnemies ?? [];
        var candidates = world
            .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat))
            .Cards
            .Where(minion => !activated.Contains(minion.ObjectId))
            .Select(minion => minion.ObjectId)
            .OrderBy(id => id)
            .ToList();
        var request = new TargetRequest(
            candidates, candidates.Count, candidates.Count, Rule: "rr:minion.3");

        if (input.IsDecline
            || input.Affordance != villain.ObjectId
            || !request.Allows(input.Targets))
        {
            throw new RulesNotImplementedException(
                $"player {seat} must order every engaged minion activation; "
                + $"offered [{string.Join(',', candidates)}], chose "
                + $"[{string.Join(',', input.Targets)}], affordance "
                + $"{input.Affordance} (expected {villain.ObjectId})");
        }

        world.Agenda.Then(step with
        {
            ActivationOrder = [.. input.Targets],
            ProcedureCandidates = [.. candidates],
            OccurrenceId = null,
        });
    }

}
