using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One visibility-safe, human-readable entry in the game chronology.</summary>

/// <summary>Formats completed actions without exposing raw event diagnostics.</summary>
public static class ActionHistoryPresenter
{
    /// <summary>Builds one complete action entry from authorized engine facts.</summary>
    public static ActionHistoryPresentation PresentEntry(
        ActionHistoryFacts action,
        IReadOnlyList<GameEvent> events,
        WorldDescriptor world)
    {
        string summary = Present(action);
        IReadOnlyList<string> details = PresentDiscardDetails(action, events, world);
        bool discardIsRootChoice = action.Phase is "Mulligan" or "EndPhase";
        if (action.Outcome is null && discardIsRootChoice && details.Count > 0)
        {
            summary = details[0];
            details = details.Skip(1).ToArray();
        }
        return new ActionHistoryPresentation(summary, details);
    }

    /// <summary>Returns one player-facing sentence for a completed history unit.</summary>
    public static string Present(ActionHistoryFacts action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(action.Actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(action.Action);
        ArgumentNullException.ThrowIfNull(action.ResourceGeneratorIds);
        ArgumentNullException.ThrowIfNull(action.ResourceGenerators);

        string summary = Summary(action);
        return action.Outcome is { } outcome
            ? $"{summary} {EventPresenter.Terminal(outcome).Summary}"
            : summary;
    }

    private static string Summary(ActionHistoryFacts action)
    {
        if (string.Equals(action.Verb, CardPlay.Verb, StringComparison.Ordinal))
        {
            return PlaySummary(action);
        }
        else if (string.Equals(action.Verb, Game.ChangeForm, StringComparison.Ordinal))
        {
            return $"{action.Actor} changed form.";
        }
        else if (string.Equals(action.Verb, Game.EndPhaseVerb, StringComparison.Ordinal)
            && string.Equals(action.Phase, "PlayerTurn", StringComparison.Ordinal))
        {
            return $"{action.Actor} ended their turn.";
        }
        else if (string.Equals(action.Verb, BasicPowers.AttackVerb, StringComparison.Ordinal))
        {
            return BasicPowerSummary(action, "attacked");
        }
        else if (string.Equals(action.Verb, BasicPowers.ThwartVerb, StringComparison.Ordinal))
        {
            return BasicPowerSummary(action, "thwarted");
        }
        else if (string.Equals(action.Verb, BasicPowers.RecoverVerb, StringComparison.Ordinal))
        {
            return $"{action.Actor} recovered.";
        }
        else
        {
            return OtherSummary(action);
        }
    }

    private static string PlaySummary(ActionHistoryFacts action)
    {
        string payment = action.ResourceGenerators.Count == 0
            ? string.Empty
            : $", generating resources from {Names(action.ResourceGenerators)}";
        return $"{action.Actor} played {action.Action}{payment}.";
    }

    private static string OtherSummary(ActionHistoryFacts action)
    {
        string choice = action.Action.Trim().TrimEnd('.');
        string phase = Words(action.Phase);
        string phaseName = phase.EndsWith(" phase", StringComparison.Ordinal)
            ? phase
            : $"{phase} phase";
        return action.Role == "phase_step"
            || !string.Equals(action.Phase, "PlayerTurn", StringComparison.Ordinal)
            ? $"{action.Actor} resolved {choice} during the {phaseName}."
            : $"{action.Actor} used {choice}.";
    }

    private static string BasicPowerSummary(ActionHistoryFacts action, string verb) =>
        string.Equals(action.Actor, action.Action, StringComparison.Ordinal)
            ? $"{action.Actor} {verb}."
            : $"{action.Actor} {verb} with {action.Action}.";

    /// <summary>
    /// Describes genuine discard results while omitting cards spent for, or
    /// moved as part of, the summarized play action.
    /// </summary>
    public static IReadOnlyList<string> PresentDiscardDetails(
        ActionHistoryFacts action,
        IReadOnlyList<GameEvent> events,
        WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(world);
        var mechanics = action.ResourceGeneratorIds.ToHashSet();
        if (string.Equals(action.Verb, CardPlay.Verb, StringComparison.Ordinal)
            && action.Subject is int subject)
        {
            mechanics.Add(subject);
        }

        CardsMoved[] discards = events
            .OfType<CardsMoved>()
            .Where(moved => string.Equals(moved.Verb, "Discard", StringComparison.Ordinal))
            .Select(moved => moved with
            {
                Cards = moved.Cards.Where(card => !mechanics.Contains(card.Card)).ToArray(),
            })
            .Where(moved => moved.Cards.Count > 0)
            .ToArray();
        var combined = new List<CardsMoved>(discards.Length);
        foreach (CardsMoved moved in discards)
        {
            if (combined.LastOrDefault() is CardsMoved prior
                && SameArea(prior.From, moved.From)
                && SameArea(prior.To, moved.To)
                && string.Equals(prior.Trigger, moved.Trigger, StringComparison.Ordinal))
            {
                combined[^1] = prior with
                {
                    Cards = prior.Cards.Concat(moved.Cards).ToArray(),
                };
                continue;
            }
            combined.Add(moved);
        }
        return combined.Select(moved => HistoryDiscardSummary(moved, world)).ToArray();
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

    private static string Names(IReadOnlyList<string> names) => names.Count switch
    {
        1 => names[0],
        2 => $"{names[0]} and {names[1]}",
        _ => $"{string.Join(", ", names.Take(names.Count - 1))}, and {names[^1]}",
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
