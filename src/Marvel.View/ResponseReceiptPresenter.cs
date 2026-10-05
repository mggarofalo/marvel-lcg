using Marvel.Rules.Events;

namespace Marvel.View;

/// <summary>Explains one accepted response, independently of its enclosing journal unit.</summary>
public static class ResponseReceiptPresenter
{
    /// <summary>Retains the current commitment, payment and meaningful results in resolution order.</summary>
    public static IReadOnlyList<EventPresentation> Present(
        IReadOnlyList<GameEvent> events, WorldDescriptor world,
        EventBatchPresentation batch, DecisionReceiptContext? accepted = null)
    {
        HashSet<int> played = [.. events.OfType<CardsMoved>()
            .Where(moved => moved.Verb == "Play" && moved.From.Zone == "HandsArea")
            .SelectMany(moved => moved.Cards.Select(card => card.Card))];
        var results = new List<GameEvent>();
        foreach (GameEvent happened in events)
        {
            if (happened is FieldSet field && !EventFieldPresentation.IsReceiptRelevant(field)) continue;
            if (happened is CardsMoved { Verb: "Discard", From.Zone: "RevealingArea" } cleanup)
            {
                var remaining = cleanup.Cards.Where(card => !played.Contains(card.Card)).ToArray();
                if (remaining.Length > 0) results.Add(cleanup with { Cards = remaining });
            }
            else results.Add(happened);
        }
        IEnumerable<EventPresentation> current = events.Count == 0
            ? batch.Highlights : EventPresenter.PresentNarrative(results, world);
        var commitment = accepted is null ? Array.Empty<EventPresentation>()
            : new[] { new EventPresentation(accepted.Commitment, "Accepted decision",
                accepted.Anchors, EventMotionKind.State) };
        return [.. commitment.Concat(current)
            .Concat(batch.History.Where(item => item.Motion == EventMotionKind.Terminal))
            .DistinctBy(item => item.Summary)];
    }
}
