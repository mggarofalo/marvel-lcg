using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;

namespace Marvel.View;

/// <summary>Formats action results without restoring concealed historical identities.</summary>
internal static class ActionHistoryResultPresentation
{
    /// <summary>
    /// Describes genuine discard results while omitting cards spent for, or
    /// moved as part of, the summarized play action.
    /// </summary>
    internal static IReadOnlyList<string> Discards(
        ActionHistoryFacts action,
        IReadOnlyList<GameEvent> events,
        WorldDescriptor world)
        => Details(action, events, world, discardsOnly: true);

    internal static IReadOnlyList<string> Present(
        ActionHistoryFacts action,
        IReadOnlyList<GameEvent> events,
        WorldDescriptor world) => Details(action, events, world, discardsOnly: false);

    private static string[] Details(
        ActionHistoryFacts action,
        IReadOnlyList<GameEvent> events,
        WorldDescriptor world,
        bool discardsOnly)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(world);
        CardsMoved[] results = EffectMoves(action, events, discardsOnly);
        return CombineMoves(results).Select(moved => EventMovementPresentation.IsEffectHandGain(moved)
            ? HistoryHandGainSummary(moved, world)
            : HistoryDiscardSummary(moved, world)).ToArray();
    }

    private static CardsMoved[] EffectMoves(
        ActionHistoryFacts action, IReadOnlyList<GameEvent> events, bool discardsOnly)
    {
        var mechanics = action.ResourceGeneratorIds.ToHashSet();
        if (string.Equals(action.Verb, CardPlay.Verb, StringComparison.Ordinal)
            && action.Subject is int subject)
        {
            mechanics.Add(subject);
        }

        return events
            .OfType<CardsMoved>()
            .Where(moved => string.Equals(moved.Verb, "Discard", StringComparison.Ordinal)
                || (!discardsOnly && EventMovementPresentation.IsEffectHandGain(moved)))
            .Select(moved => moved with
            {
                Cards = string.Equals(moved.Verb, "Discard", StringComparison.Ordinal)
                    ? moved.Cards.Where(card => !mechanics.Contains(card.Card)).ToArray()
                    : moved.Cards,
            })
            .Where(moved => moved.Cards.Count > 0)
            .ToArray();
    }

    private static List<CardsMoved> CombineMoves(CardsMoved[] moves)
    {
        var combined = new List<CardsMoved>(moves.Length);
        foreach (CardsMoved moved in moves)
        {
            if (combined.LastOrDefault() is CardsMoved prior
                && SameArea(prior.From, moved.From)
                && SameArea(prior.To, moved.To)
                && string.Equals(prior.Trigger, moved.Trigger, StringComparison.Ordinal)
                && string.Equals(prior.Verb, moved.Verb, StringComparison.Ordinal))
            {
                combined[^1] = prior with
                {
                    Cards = prior.Cards.Concat(moved.Cards).ToArray(),
                };
                continue;
            }
            combined.Add(moved);
        }
        return combined;
    }

    private static string HistoryHandGainSummary(CardsMoved moved, WorldDescriptor world)
    {
        string actor = EventSubjectDescriptions.Player(moved.To.Owner, world);
        string verb = string.Equals(moved.Verb, "Return", StringComparison.Ordinal)
            ? "returned" : "added";
        return $"{actor} {verb} {HistoryCards(moved, world)} to their hand"
            + $" from {HistoryArea(moved.From, world)}.";
    }

    private static string HistoryDiscardSummary(CardsMoved moved, WorldDescriptor world)
    {
        string actor = moved.From.Owner < 0
            ? "The scenario"
            : world.Players.FirstOrDefault(player => player.Seat == moved.From.Owner)?.Name
                ?? $"Player {moved.From.Owner + 1}";
        string cards = HistoryCards(moved, world);
        return string.Equals(moved.From.Zone, "HandsArea", StringComparison.Ordinal)
            ? $"{actor} discarded {cards}."
            : $"{actor} discarded {cards} from {HistoryArea(moved.From, world)}.";
    }

    private static string HistoryCards(CardsMoved moved, WorldDescriptor world)
    {
        string fallback = moved.From.Owner < 0 ? "an encounter card" : "a player card";
        string[] names = moved.Cards.Select(landing =>
        {
            CardDescriptor? card = world.Areas
                .SelectMany(area => area.Cards.Concat(area.Removed))
                .FirstOrDefault(candidate => candidate.Id == landing.Card);
            return card?.Face?.Title ?? (card is null
                ? fallback
                : $"a face-down {card.Back.ToString().ToLowerInvariant()} card");
        }).ToArray();
        if (names.Length > 1 && names.All(name => string.Equals(
                name, fallback, StringComparison.Ordinal)))
        {
            return moved.From.Owner < 0
                ? $"{names.Length} encounter cards"
                : $"{names.Length} player cards";
        }
        return Names(names);
    }

    private static string HistoryArea(AreaRef area, WorldDescriptor world)
    {
        string zone = area.Zone.EndsWith("Area", StringComparison.Ordinal)
            ? area.Zone[..^"Area".Length]
            : area.Zone;
        string name = Words(zone);
        if (area.Owner < 0)
        {
            return $"the scenario's {name}";
        }
        string player = world.Players.FirstOrDefault(candidate => candidate.Seat == area.Owner)?.Name
            ?? $"player {area.Owner + 1}";
        return $"{player}'s {name}";
    }

    private static bool SameArea(AreaRef left, AreaRef right) =>
        left.Owner == right.Owner
        && left.Host == right.Host
        && string.Equals(left.Zone, right.Zone, StringComparison.Ordinal)
        && string.Equals(left.Id, right.Id, StringComparison.Ordinal);

    private static string Names(string[] names) => names.Length switch
    {
        1 => names[0],
        2 => $"{names[0]} and {names[1]}",
        _ => $"{string.Join(", ", names.Take(names.Length - 1))}, and {names[^1]}",
    };

    private static string Words(string value)
    {
        var words = new StringBuilder(value.Length + 4);
        for (int index = 0; index < value.Length; index++)
        {
            if (index > 0 && char.IsUpper(value[index]) && char.IsLower(value[index - 1]))
            {
                words.Append(' ');
            }
            words.Append(char.ToLowerInvariant(value[index]));
        }
        return words.ToString();
    }
}
