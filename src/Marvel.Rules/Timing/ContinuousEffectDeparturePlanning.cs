using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Plans complete hosted-card departure trees.</summary>
internal static class ContinuousEffectDeparturePlanning
{
    internal static void AddDeparture(this ContinuousEffects effects,
        Card root, bool includeHostedCards, List<Card> planned, HashSet<int> plannedIds)
    {
        var path = new HashSet<int>();
        var pending = new Stack<(Card Card, bool Exit)>();
        pending.Push((root, false));
        while (pending.TryPop(out var frame))
        {
            var card = frame.Card;
            if (frame.Exit)
            {
                path.Remove(card.ObjectId);
                continue;
            }

            if (path.Contains(card.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"attachment {card.ObjectId} forms a hosting cycle");
            }
            if (!plannedIds.Add(card.ObjectId))
            {
                // Another root already planned this complete subtree. Sharing
                // a plan is not a hosting cycle; only revisiting the current
                // ancestor path is.
                continue;
            }

            planned.Add(card);
            if (!includeHostedCards)
            {
                continue;
            }

            path.Add(card.ObjectId);
            pending.Push((card, true));
            foreach (var child in effects.world.Areas
                         .Where(area => area.Host == card.ObjectId)
                         .SelectMany(area => area.Cards)
                         .Reverse())
            {
                pending.Push((child, false));
            }
        }
    }
}
