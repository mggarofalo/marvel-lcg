using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Saveable orchestration for card text that resolves for each player.</summary>
public static class EachPlayerEffects
{
    /// <summary>Schedule frames from an opaque continuation template built by Cards.</summary>
    public static void Schedule(World world, PhaseStep template)
    {
        ArgumentNullException.ThrowIfNull(world);
        var players = world.PlayerOrder.ToList();
        if (players.Count == 0) return;
        if (players.Count == 1)
        {
            ScheduleFrames(world, template, players);
            return;
        }
        world.Agenda.Then(template with
        {
            What = Steps.OrderEachPlayer, Round = world.Agenda.Current?.Round ?? 0,
            Number = 2, Seat = world.FirstPlayer, Plan = true,
        });
    }

    internal static Prompt Ordering(World world, PhaseStep step)
    {
        var source = Source(world, step);
        var identities = world.PlayerOrder
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
        var candidates = world.PlayerOrder
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

        ScheduleFrames(world, step, input.Targets.Select(identity => candidates[identity]).ToList());
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
            return [];
        }

        return abilities.ResolveEachPlayer(
            world,
            source,
            step.Seat,
            step.Index,
            step.Tier,
            step.FinalStep,
            step.FinalPlayer);
    }

    private static void ScheduleFrames(World world, PhaseStep template, List<int> players)
    {
        int round = world.Agenda.Current?.Round ?? 0;
        for (int position = 0; position < players.Count; position++)
        {
            world.Agenda.Then(template with
            {
                What = Steps.ResolveEachPlayer, Round = round, Number = 2,
                Seat = players[position], Plan = true,
                FinalPlayer = position == players.Count - 1, EachPlayerFrame = true,
            });
        }
    }

    private static Card Source(World world, PhaseStep step) =>
        step.Subject >= 0 && step.Subject < world.Cards.Count
            ? world.Cards[step.Subject]
            : throw new RulesNotImplementedException(
                $"each-player frame has no card at object id {step.Subject}");
}
