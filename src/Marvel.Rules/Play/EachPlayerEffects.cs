using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Saveable orchestration for card text that resolves for each player.</summary>
public static class EachPlayerEffects
{
    /// <summary>Schedule one independently resumable frame per player.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:each-player.1</c>: "If an 'each player' effect does not specify an
    /// order in which to resolve the effect, the first player determines the
    /// order in which the effect resolves." A single player has only one legal
    /// order, so that frame is scheduled without asking a vacuous question.
    /// </para>
    /// <para>
    /// The order is stored as agenda frames rather than an iterator. Every
    /// frame carries its own seat and reconstruction position, so a choice that
    /// suspends one player's effect cannot lend its context to the next player.
    /// The field spelling is an engine choice; the rulebook only determines
    /// who orders the resolutions.
    /// </para>
    /// </remarks>
    public static void Schedule(
        World world, Card source, int stoppedAt, AbilityType? tier = null,
        bool finalStep = false, bool surgeGained = false, int abilityOrdinal = -1,
        IReadOnlyList<string>? abilityPath = null, string abilityFace = "",
        int abilityPlayer = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);

        var players = world.PlayerOrder.ToList();
        if (players.Count == 0)
        {
            return;
        }

        int round = world.Agenda.Current?.Round ?? 0;
        if (players.Count == 1)
        {
            ScheduleFrames(
                world, source, stoppedAt, tier, finalStep, surgeGained, players,
                abilityOrdinal, abilityPath, abilityFace, abilityPlayer);
            return;
        }

        world.Agenda.Then(new PhaseStep(
            Steps.OrderEachPlayer,
            round,
            2,
            Index: stoppedAt,
            Subject: source.ObjectId,
            Seat: world.FirstPlayer,
            Plan: true,
            Tier: tier,
            FinalStep: finalStep,
            SurgeGained: surgeGained,
            AbilityOrdinal: abilityOrdinal,
            AbilityPath: abilityPath,
            AbilityFace: abilityFace,
            AbilityPlayer: abilityPlayer));
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

        ScheduleFrames(
            world,
            source,
            step.Index,
            step.Tier,
            step.FinalStep,
            step.SurgeGained,
            input.Targets.Select(identity => candidates[identity]).ToList(),
            step.AbilityOrdinal,
            step.AbilityPath,
            step.AbilityFace,
            step.AbilityPlayer);
    }

    internal static IReadOnlyList<GameEvent> Resolve(
        World world, ICardAbilities abilities, PhaseStep step)
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

    private static void ScheduleFrames(
        World world,
        Card source,
        int stoppedAt,
        AbilityType? tier,
        bool finalStep,
        bool surgeGained,
        List<int> players,
        int abilityOrdinal,
        IReadOnlyList<string>? abilityPath,
        string abilityFace,
        int abilityPlayer)
    {
        int round = world.Agenda.Current?.Round ?? 0;
        for (int position = 0; position < players.Count; position++)
        {
            world.Agenda.Then(new PhaseStep(
                Steps.ResolveEachPlayer,
                round,
                2,
                Index: stoppedAt,
                Subject: source.ObjectId,
                Seat: players[position],
                Plan: true,
                Tier: tier,
                FinalStep: finalStep,
                FinalPlayer: position == players.Count - 1,
                EachPlayerFrame: true,
                SurgeGained: surgeGained,
                AbilityOrdinal: abilityOrdinal,
                AbilityPath: abilityPath,
                AbilityFace: abilityFace,
                AbilityPlayer: abilityPlayer));
        }
    }

    private static Card Source(World world, PhaseStep step) =>
        step.Subject >= 0 && step.Subject < world.Cards.Count
            ? world.Cards[step.Subject]
            : throw new RulesNotImplementedException(
                $"each-player frame has no card at object id {step.Subject}");
}
