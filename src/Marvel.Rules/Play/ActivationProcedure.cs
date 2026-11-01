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
        var playerOrder = step.Require<ActivationPlanOperation>().Players ?? world.PlayerOrder.ToList();
        if (step.Index >= playerOrder.Count)
        {
            return null;
        }

        int seat = playerOrder[step.Index];
        var activated = step.Require<ActivationPlanOperation>().ActivatedEnemies ?? [];

        // An eliminated seat remains in this procedure's stable order so its
        // removal cannot shift the next player under the current index. It has
        // no enemies left to activate, and no game area to find a villain in
        // (pack:mc11:game-areas); advance and clear the per-player set.
        if (world.Seats[seat].Eliminated)
        {
            world.Agenda.Then(step with
            {
                Operation = step.Require<ActivationPlanOperation>() with
                {
                    ActivatedEnemies = [],
                    Players = playerOrder
                },
                Index = step.Index + 1,
                OccurrenceId = null
            });
            return null;
        }

        var villain = EncounterRouting.VillainForPlayer(world, seat);
        var remaining = MinionActivationChoice.Remaining(world, seat, step);
        bool villainActivated = villain is null || HasActivatedVillain(world, villain, activated);
        if (NeedsOrder(step, villainActivated, remaining))
            return MinionActivationChoice.Describe(world, villain, seat, remaining);

        int? enemy = NextEnemy(step, villain, villainActivated, remaining);

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
            Operation = step.Require<ActivationPlanOperation>() with
            {
                ActivatedEnemies = [],
                Players = playerOrder,
                Chosen = null,
                Incarnations = null
            },
            Index = step.Index + 1,
            OccurrenceId = null
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
            attacking ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            step.Round,
            2,
            Index: seat,
            Subject: next,
            Seat: seat
        ));
        world.Agenda.Then(step with { Operation = step.Require<ActivationPlanOperation>() with { ActivatedEnemies = [.. step.Require<ActivationPlanOperation>().ActivatedEnemies ?? [], next], Incarnations = new Dictionary<int, long>(step.Require<ActivationPlanOperation>().Incarnations
                ?? new Dictionary<int, long>()) { [next] = world.Cards[next].Incarnation }, Players = playerOrder, Chosen = null }, OccurrenceId = null });
    }

    private static bool NeedsOrder(
        PhaseStep step, bool villainActivated, List<Card> remaining) =>
        villainActivated
        && step.Require<ActivationPlanOperation>().Chosen?.Any(id => remaining.Any(card => card.ObjectId == id)) != true
        && remaining.Count > 1;

    private static int? NextEnemy(
        PhaseStep step, Card? villain, bool villainActivated, List<Card> remaining)
    {
        if (!villainActivated) return villain!.ObjectId;
        return step.Require<ActivationPlanOperation>().Chosen?.Where(id => remaining.Any(card => card.ObjectId == id))
            .Select(id => (int?)id).FirstOrDefault()
            ?? remaining.Select(card => (int?)card.ObjectId).FirstOrDefault();
    }

    private static bool HasActivatedVillain(World world, Card villain, IReadOnlyList<int> activated) =>
        // pack:mc03:new-rules limits the ordinary villain-phase activation to
        // the active villain. Moving the counter does not grant a second one.
        world.EncounterTopology is { } topology
            ? topology.Groups.Any(group => group.Stages.Any(stage => activated.Contains(stage.ObjectId)))
            : activated.Contains(villain.ObjectId);
}
