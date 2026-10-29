using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

internal static class PlayerAllyEntry
{
    internal static void SchedulePlay(World world, Seat seat, Card card, CardEntryChoices choices) =>
        Schedule(world, new PlayerAllyEntryOperation(card, seat.Index, choices, null));

    internal static void SchedulePut(World world, AllyEntryRequest request) =>
        Schedule(world, new PlayerAllyEntryOperation(request.Ally, request.Controller, null, request));

    private static void Schedule(World world, PlayerAllyEntryOperation entry)
    {
        var current = world.Agenda.Current;
        var step = new PhaseStep(entry, current?.Round ?? 0, 0, Subject: entry.Card.ObjectId, Seat: entry.Player);
        if (current is null) world.Agenda.Add(step);
        else world.Agenda.Then(step);
    }

    internal static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        var entry = step.Require<PlayerAllyEntryOperation>();
        if (!entry.MatchesPending(entry.Card))
            throw new RulesNotImplementedException("the pending ally changed before entry completed");
        void CarryCounters()
        {
            foreach (var (key, count) in entry.Counters)
            {
                if (entry.Card.Tokens.GetValueOrDefault(key) < count)
                    throw new RulesNotImplementedException("staged entry counters changed before movement");
            }
        }
        void RestoreCounters()
        {
            foreach (var (key, count) in entry.Counters) entry.Card.PlaceTokens(key, count);
            entry.Put?.Binding?.Capture(entry.Card);
        }
        CarryCounters();
        if (entry.Choices is { } choices)
            CardEntry.Enter(world, world.Facts, world.CardPlayAbilities, world.Seats[entry.Player], entry.Card, events, choices with { ApplyingAlly = true, AfterMove = RestoreCounters });
        else
            AllyCardEntry.Put(world, world.Facts, world.CardPlayAbilities, entry.Put!, events, applying: true, afterMove: RestoreCounters);
        var occurrence = world.Agenda.Occurrence ?? throw new InvalidOperationException("ally entry has no occurrence");
        world.Agenda.BeforeResponses(step, occurrence);
        return null;
    }
}
