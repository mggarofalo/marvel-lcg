using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Keeps a played event faceup until all of its resolving work finishes.</summary>
internal static class EventCardEntry
{
    internal static void Resolve(World world, ICardPlayAbilities abilities, Seat seat,
        Card card, List<GameEvent> events)
    {
        world.Agenda.Executions.RunBatch(
            new Occurrence(0, [CardPlay.Verb], Subject: card.ObjectId, Player: seat.Index),
            execution => Begin(world, abilities, seat, card, events, execution));
    }

    private static AbilityExecutionResult Begin(World world, ICardPlayAbilities abilities, Seat seat,
        Card card, List<GameEvent> events, AbilityExecution execution)
    {
        // rr:play-put-into-play.2: "resolve its ability, and place the card in its owner's discard pile."
        PlayedCardExposure.Expose(world, card, seat.Index, events);
        var result = abilities.ExecuteReveal(world, card, seat.Index);
        ActivityBatches.Append(events, result.Events);
        var completion = new EventCardCompletionOperation(world, card, card.Incarnation, result, execution);
        if (result.IsComplete) return Complete(world, completion, events);
        world.Agenda.ThenContinuation(
            new PhaseStep(completion, world.Agenda.Current?.Round ?? 0, 0), execution.Occurrence);
        return execution.Yield(events);
    }

    internal static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        world.Agenda.Executions.RequireContinuation(step);
        var completion = step.Require<EventCardCompletionOperation>();
        world.Agenda.Executions.Resume(completion.Execution);
        Complete(world, completion, events);
        return null;
    }

    private static AbilityExecutionResult Complete(
        World world, EventCardCompletionOperation completion, List<GameEvent> events)
    {
        if (!ReferenceEquals(world, completion.World) || completion.Consumed)
            throw new InvalidOperationException("event completion requires its unused owning game");
        if (!completion.Resolution.IsComplete)
            throw new InvalidOperationException("played event finalized before its ability completed");
        completion.Consumed = true;
        var card = completion.Card;
        if (card.Incarnation == completion.Incarnation && card.Area.Type == DeckType.RevealingArea)
            Discard.Card(world, card, CardPlay.Verb, events);
        return completion.Execution.Finish(events);
    }
}
