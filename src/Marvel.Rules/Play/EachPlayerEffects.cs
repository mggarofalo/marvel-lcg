using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Saveable orchestration for card text that resolves for each player.</summary>
public static class EachPlayerEffects
{
    /// <summary>Schedule frames from an opaque continuation template built by Cards.</summary>
    public static void Schedule(World world, PhaseStep template, int? onlyPlayer = null, bool inPlayerOrder = false)
    {
        ArgumentNullException.ThrowIfNull(world);
        var players = Players(world, Source(world, template))
            .Where(player => onlyPlayer is null || player == onlyPlayer).ToList();
        if (players.Count == 0) return;
        // rr:each-player.1 permits the first player to choose only when card
        // text does not specify an order; rr:in-player-order starts at first player.
        if (players.Count == 1 || inPlayerOrder)
        {
            ScheduleFrames(world, template, players);
            return;
        }
        world.Agenda.Then(template with
        {
            Operation = template.Require<AbilityContinuationAgendaOperation>() with { Stage = AbilityContinuationStage.OrderPlayers },
            Round = world.Agenda.Current?.Round ?? 0,
            Number = 2,
            Seat = world.FirstPlayer,
            Plan = true,
        });
    }

    /// <summary>The players in the game this card's "each player" means, in player order.</summary>
    /// <remarks>
    /// Ruling 4799b99ecc0e5dea (<c>pack:mc11:rules-clarifications</c>): "'Each
    /// player' refers to each player in the same game area." <see cref="Places.EachPlayer"/>
    /// owns that partition; <see cref="World.PlayerOrder"/> supplies the order and
    /// leaves out eliminated players (<c>rr:player-elimination.6</c>). In an
    /// ordinary game every player shares the source's game area.
    /// </remarks>
    public static IReadOnlyList<int> Players(World world, Card source)
    {
        ArgumentNullException.ThrowIfNull(world);
        var reached = Places.EachPlayer(world, source);
        return [.. world.PlayerOrder.Where(reached.Contains)];
    }

    internal static Prompt Ordering(World world, PhaseStep step)
    {
        var source = Source(world, step);
        var identities = Players(world, source)
            .Select(seat => world.Seats[seat].IdentityCard.ObjectId)
            .ToList();

        return new Prompt(
            Player: world.FirstPlayer,
            Asking: Question.Order,
            When: TimingPriority.Untimed,
            Trigger: Steps.OrderEachPlayer,
            Label: $"order the players for {source.FaceId}",
            Cancellable: false,
            Affordances:
            [
                new Affordance(
                    source.ObjectId,
                    "Order",
                    source.ObjectId,
                    world.FirstPlayer,
                    "each player",
                    new TargetRequest(
                        identities,
                        identities.Count,
                        identities.Count,
                        Rule: "rr:each-player.1")),
            ]);
    }

    internal static void Ordered(World world, PhaseStep step, Decision input)
    {
        var source = Source(world, step);
        var candidates = Players(world, source)
            .ToDictionary(
                seat => world.Seats[seat].IdentityCard.ObjectId,
                seat => seat);
        var request = new TargetRequest(
            [.. candidates.Keys],
            candidates.Count,
            candidates.Count,
            Rule: "rr:each-player.1");

        if (input.IsDecline
            || input.Affordance != source.ObjectId
            || !request.Allows(input.Targets))
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' needs an exact ordering of every player in the game");
        }

        var invocation = step.ExecutionOwner;
        if (invocation is not null) world.Agenda.Executions.Resume(invocation);
        ScheduleFrames(world, step, input.Targets.Select(identity => candidates[identity]).ToList());
        if (invocation is not null) world.Agenda.Executions.Suspend(invocation);
    }

    internal static IReadOnlyList<GameEvent> Resolve(
        World world, ICardContinuationAbilities abilities, PhaseStep step)
    {
        var source = Source(world, step);
        if (step.Seat < 0 || step.Seat >= world.Seats.Count || world.Seats[step.Seat].Eliminated)
        {
            // `rr:player-elimination.6`: effects referring to players in the
            // game ignore an eliminated player. A player can be eliminated
            // after the first player chose the order but before their frame.
            if (step.ExecutionOwner is { } invocation)
            {
                world.Agenda.Executions.Resume(invocation);
                world.Agenda.Executions.Complete(invocation);
            }
            return [];
        }

        return abilities.ResolveEachPlayer(world, step);
    }

    private static void ScheduleFrames(World world, PhaseStep template, List<int> players)
    {
        int round = world.Agenda.Current?.Round ?? 0;
        for (int position = 0; position < players.Count; position++)
        {
            world.Agenda.Then(template with
            {
                Operation = template.Require<AbilityContinuationAgendaOperation>() with
                { Stage = AbilityContinuationStage.ResolvePlayer, FinalPlayer = position == players.Count - 1 },
                Round = round,
                Number = 2,
                Seat = players[position],
                Plan = true,
            });
        }
    }

    private static Card Source(World world, PhaseStep step) =>
        step.Subject >= 0 && step.Subject < world.Cards.Count
            ? world.Cards[step.Subject]
            : throw new RulesNotImplementedException(
                $"each-player frame has no card at object id {step.Subject}");
}
