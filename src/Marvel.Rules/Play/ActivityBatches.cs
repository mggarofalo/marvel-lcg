using Marvel.Rules.Events;

namespace Marvel.Rules.Play;

/// <summary>Records semantic instruction boundaries without changing low-level resolution order.</summary>
public static class ActivityBatches
{
    /// <summary>Captures matching movements or ready-state changes produced by one instruction.</summary>
    /// <remarks>Event offsets are deterministic response-local ids, an engine representation choice.</remarks>
    public static void Complete(List<GameEvent> events, int start, string verb,
        int player, ActivityPhaseStep? phaseStep = null, IReadOnlyCollection<int>? included = null)
    {
        int batch = FirstMember(events, start, verb, included);
        if (batch < 0) return;
        var participants = new List<ActivityParticipant>();
        var cardsByPlayer = new Dictionary<int, List<int>>();
        var names = new Dictionary<int, string>();
        Capture(events, batch, verb, player, included, cardsByPlayer, names);
        // Insertion order follows the explicit resolution walk, never dictionary iteration.
        foreach (int seat in events.Skip(batch).Where(item => item.ActivityBatch == batch)
            .Select(item => player >= 0 ? player : item is CardsMoved moved ? moved.To.Owner : -1).Distinct())
        {
            List<int> cards = cardsByPlayer[seat];
            participants.Add(new ActivityParticipant(seat, cards.Count, cards.ToArray()));
        }
        if (participants.Count == 0) return;
        events.Add(new ActivityCompleted(batch, participants, phaseStep)
        {
            Verb = verb,
            Trigger = events[batch].Trigger,
            Subjects = names.Count == 0 ? null : names,
        });
    }

    private static int FirstMember(List<GameEvent> events, int start, string verb,
        IReadOnlyCollection<int>? included)
    {
        for (int index = start; index < events.Count; index++)
            if (events[index].ActivityBatch is null && Members(events[index], verb)
                .Any(card => included is null || included.Contains(card))) return index;
        return -1;
    }

    private static void Capture(List<GameEvent> events, int start, string verb, int player,
        IReadOnlyCollection<int>? included, Dictionary<int, List<int>> cardsByPlayer,
        Dictionary<int, string> names)
    {
        for (int index = start; index < events.Count; index++)
        {
            GameEvent happened = events[index];
            if (happened.ActivityBatch is not null) continue;
            int[] cards = Members(happened, verb).Where(card => included is null || included.Contains(card)).ToArray();
            if (cards.Length == 0) continue;
            int seat = player >= 0 ? player : happened is CardsMoved moved ? moved.To.Owner : -1;
            if (!cardsByPlayer.TryGetValue(seat, out List<int>? owned))
            {
                owned = [];
                cardsByPlayer.Add(seat, owned);
            }
            owned.AddRange(cards);
            CaptureNames(happened, cards, names);
            events[index] = happened with { ActivityBatch = start };
        }
    }

    private static void CaptureNames(GameEvent happened, int[] cards, Dictionary<int, string> names)
    {
        if (happened.Subjects is not { } subjects) return;
        foreach (int card in cards)
            if (subjects.TryGetValue(card, out string? name)) names[card] = name;
    }

    private static int[] Members(GameEvent happened, string verb) => happened switch
    {
        CardsMoved moved when moved.Verb == verb => moved.Cards.Select(card => card.Card).ToArray(),
        FieldSet set when verb == "Ready" && set.Verb == verb && set.Field == "is_exhaust" => [set.Card],
        _ => [],
    };
}
