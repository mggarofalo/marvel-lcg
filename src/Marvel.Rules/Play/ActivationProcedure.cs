using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Resolves each player's villain activation and live remaining minion activations.</summary>

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
        MinionActivationChoice.Answer(world, step, input);
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

        var remaining = MinionActivationChoice.Remaining(world, seat, step);
        if (NeedsOrder(step, villain, activated, remaining))
            return MinionActivationChoice.Describe(world, villain, seat, remaining);

        int? enemy = NextEnemy(step, villain, activated, remaining);

        if (enemy is { } next)
        {
            ScheduleNext(world, facts, step, playerOrder, seat, next);
            return null;
        }

        // `rr:minion.4`: a minion that becomes engaged while engaged minions
        // are activating joins this procedure. The continuation above therefore
        // re-reads the area only after the preceding activation has completely
        // resolved. The list prevents a surviving minion from being chosen
        // again. The next choice sees every currently engaged, unactivated minion.
        world.Agenda.Then(step with
        {
            Index = step.Index + 1,
            ActivatedEnemies = [],
            ActivationPlayers = playerOrder,
            ActivationOrder = null,
            ProcedureAmounts = null,
            OccurrenceId = null,
        });
        return null;
    }

    private static void ScheduleNext(World world, ICardFacts facts, PhaseStep step,
        IReadOnlyList<int> playerOrder, int seat, int next)
    {
        // `rr:activation.1`: hero form and the enemy attacks, alter-ego form
        // and it schemes. Read the form immediately before each activation:
        // an earlier activation can change it.
        var identity = world.Seats[seat].IdentityCard;
        bool attacking = EffectiveCards.Kind(identity, facts) != CardKind.AlterEgo;

        world.Agenda.Then(new PhaseStep(
            attacking ? Steps.Attack : Steps.Scheme,
            step.Round, 2, Index: seat, Subject: next, Seat: seat));
        world.Agenda.Then(step with
        {
            ActivatedEnemies = [.. step.ActivatedEnemies ?? [], next],
            // The engine records the in-play incarnation: rr:minion.4 also
            // applies when a departed physical card engages again as a new instance.
            ProcedureAmounts = new Dictionary<int, long>(step.ProcedureAmounts
                ?? new Dictionary<int, long>()) { [next] = world.Cards[next].Incarnation },
            ActivationPlayers = playerOrder,
            ActivationOrder = null,
            OccurrenceId = null,
        });
    }

    private static bool NeedsOrder(
        PhaseStep step, Card villain, IReadOnlyList<int> activated, List<Card> remaining) =>
        activated.Contains(villain.ObjectId)
        && step.ActivationOrder?.Any(id => remaining.Any(card => card.ObjectId == id)) != true
        && remaining.Count > 1;

    private static int? NextEnemy(
        PhaseStep step, Card villain, IReadOnlyList<int> activated, List<Card> remaining)
    {
        if (!activated.Contains(villain.ObjectId)) return villain.ObjectId;
        return step.ActivationOrder?.Where(id => remaining.Any(card => card.ObjectId == id))
            .Select(id => (int?)id).FirstOrDefault()
            ?? remaining.Select(card => (int?)card.ObjectId).FirstOrDefault();
    }
}
