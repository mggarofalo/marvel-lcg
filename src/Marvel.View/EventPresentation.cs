using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.View;

/// <summary>One visibility-safe, human-readable entry in the game chronology.</summary>
public sealed record EventPresentation(
    string Summary,
    string Cause,
    IReadOnlyList<int> Anchors,
    EventMotionKind Motion);

/// <summary>The restrained visual treatment a client may apply to an event.</summary>
public enum EventMotionKind
{
    /// <summary>A card entered the visible table.</summary>
    Create,

    /// <summary>A card or area changed visible position.</summary>
    Move,

    /// <summary>A card changed which physical face is visible.</summary>
    Flip,

    /// <summary>A visible value changed in the harmful direction.</summary>
    Damage,

    /// <summary>A visible value changed in the restorative direction.</summary>
    Heal,

    /// <summary>A named counter changed.</summary>
    Counter,

    /// <summary>A status card entered or left its area.</summary>
    Status,

    /// <summary>Another visible field changed.</summary>
    State,

    /// <summary>The game reached its final outcome.</summary>
    Terminal,
}

/// <summary>Persistent entries and replaceable transient cues for one response.</summary>
public sealed record EventBatchPresentation(
    IReadOnlyList<EventPresentation> History,
    IReadOnlyList<EventPresentation> Cues);

/// <summary>Formats the closed semantic-event vocabulary without consulting engine state.</summary>
public static class EventPresenter
{
    /// <summary>Presents events in the engine-provided resolution order.</summary>
    public static IReadOnlyList<EventPresentation> Present(
        IReadOnlyList<GameEvent> events,
        WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(world);
        return events.Select(happened => Present(happened, world)).ToArray();
    }

    /// <summary>Presents one event using only its authorized response snapshot.</summary>
    public static EventPresentation Present(GameEvent happened, WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(happened);
        ArgumentNullException.ThrowIfNull(world);

        (string summary, IReadOnlyList<int> anchors, EventMotionKind motion) = happened switch
        {
            CardsCreated created => (
                $"Created {Cards(created.Cards.Select(card => card.Id), world)} in {Area(created.Area, world)}.",
                created.Cards.Select(card => card.Id).ToArray(),
                IsStatus(created.Area) ? EventMotionKind.Status : EventMotionKind.Create),
            CardsMoved moved => (
                $"Moved {Cards(moved.Cards.Select(card => card.Card), world)} from {Area(moved.From, world)} to {Area(moved.To, world)}.",
                moved.Cards.Select(card => card.Card).ToArray(),
                IsStatus(moved.From) || IsStatus(moved.To)
                    ? EventMotionKind.Status
                    : EventMotionKind.Move),
            AreaReordered reordered => (
                $"Reordered {Area(reordered.Area, world)}.",
                Array.Empty<int>(),
                EventMotionKind.Move),
            CardFormChanged changed => (
                $"{Card(changed.Card, world)} changed form.",
                [changed.Card],
                EventMotionKind.Flip),
            CardsFlipped flipped => (
                FlippedSummary(flipped, world),
                flipped.Cards.ToArray(),
                EventMotionKind.Flip),
            CardAttached attached => (
                $"Attached {Card(attached.Card, world)} to {Card(attached.Host, world)}.",
                [attached.Card, attached.Host],
                EventMotionKind.Move),
            CardDetached detached => (
                $"Detached {Card(detached.Card, world)} from {Card(detached.Host, world)}.",
                [detached.Card, detached.Host],
                EventMotionKind.Move),
            ControlChanged changed => (
                $"{Card(changed.Card, world)} changed control from {Player(changed.From, world)} to {Player(changed.To, world)}.",
                [changed.Card],
                EventMotionKind.Move),
            PlayAreaJoined joined => (
                $"{PlayArea(joined.PlayArea, world)} joined game area {joined.GameArea.ToString(CultureInfo.InvariantCulture)}.",
                Array.Empty<int>(),
                EventMotionKind.Move),
            PlayAreaDetached detached => (
                $"{PlayArea(detached.PlayArea, world)} left game area {detached.GameArea.ToString(CultureInfo.InvariantCulture)}.",
                Array.Empty<int>(),
                EventMotionKind.Move),
            FieldSet set => (
                FieldSummary(set, world),
                [set.Card],
                FieldMotion(set)),
            _ => throw new InvalidOperationException(
                $"event kind {happened.GetType().Name} has no presentation"),
        };

        return new EventPresentation(summary, Cause(happened), anchors, motion);
    }

    /// <summary>Describes a newly reached terminal state without inventing a game event.</summary>
    public static EventPresentation Terminal(Outcome outcome) => outcome switch
    {
        Outcome.VillainWins => new(
            "The villain won the game.", "Game outcome", [], EventMotionKind.Terminal),
        Outcome.PlayersLose => new(
            "The players lost the game.", "Game outcome", [], EventMotionKind.Terminal),
        Outcome.PlayersWin => new(
            "The players won the game.", "Game outcome", [], EventMotionKind.Terminal),
        _ => throw new ArgumentOutOfRangeException(
            nameof(outcome), outcome, "unfinished games have no terminal presentation"),
    };

    private static EventMotionKind FieldMotion(FieldSet set)
    {
        string field = set.Field.ToLowerInvariant();
        long? change = set.From is null || set.To is null ? null : set.To - set.From;
        if (change is null or 0)
        {
            return EventMotionKind.State;
        }

        if (field is "health" or "hitpoints" or "hit_points")
        {
            return change > 0 ? EventMotionKind.Heal : EventMotionKind.Damage;
        }

        if (field is "damage" or "k_damage")
        {
            return change < 0 ? EventMotionKind.Heal : EventMotionKind.Damage;
        }

        if (field.StartsWith("c_", StringComparison.Ordinal)
            || field == EncounterDeck.AccelerationToken)
        {
            return EventMotionKind.Counter;
        }

        return EventMotionKind.State;
    }

    private static bool IsStatus(AreaRef area) =>
        string.Equals(area.Zone, "StatusArea", StringComparison.Ordinal);

    private static string FieldSummary(FieldSet set, WorldDescriptor world)
    {
        string subject = Card(set.Card, world);
        if (set.Field == "is_exhaust")
        {
            return set.To == 1
                ? $"{subject} became exhausted."
                : $"{subject} became ready.";
        }
        string field = Words(set.Field).ToLowerInvariant();
        if (set.From is null)
        {
            return $"{subject} gained {field} {Value(set.To)}.";
        }

        if (set.To is null)
        {
            return $"{subject} lost {field} {Value(set.From)}.";
        }

        return $"{subject} changed {field} from {Value(set.From)} to {Value(set.To)}.";
    }

    private static string Value(long? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "an absent value";

    private static string FlippedSummary(CardsFlipped flipped, WorldDescriptor world)
    {
        string cards = Cards(flipped.Cards, world);
        if (!flipped.FaceUp)
        {
            return $"Turned {cards} face down.";
        }

        string[] text = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id is { } id && flipped.Cards.Contains(id))
            .Select(card => card.Face?.RulesText)
            .Where(rules => !string.IsNullOrWhiteSpace(rules))
            .Cast<string>()
            .ToArray();
        return text.Length == 0
            ? $"Turned {cards} face up."
            : $"Revealed {cards}: {string.Join(" ", text)}";
    }

    private static string Cards(IEnumerable<int> ids, WorldDescriptor world)
    {
        string[] names = ids.Select(id => Card(id, world)).ToArray();
        return names.Length switch
        {
            0 => "no cards",
            1 => names[0],
            2 => $"{names[0]} and {names[1]}",
            _ => $"{string.Join(", ", names[..^1])}, and {names[^1]}",
        };
    }

    private static string Card(int id, WorldDescriptor world)
    {
        CardDescriptor? card = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(candidate => candidate.Id == id);
        if (card?.Face is { } face)
        {
            return face.Title;
        }

        if (card is not null)
        {
            return $"face-down {card.Back.ToString().ToLowerInvariant()} card";
        }

        // An authorized event may outlive the object's presence in the resulting
        // snapshot. Its response-scoped object id is safe; a printed face is not.
        return $"card {id.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string Area(AreaRef area, WorldDescriptor world)
    {
        string name = Words(area.Zone, trimArea: true).ToLowerInvariant();
        string owner = area.Owner < 0 ? "the scenario" : Player(area.Owner, world);
        if (area.Host >= 0)
        {
            return $"{owner}'s {name} on {Card(area.Host, world)}";
        }

        return $"{owner}'s {name}";
    }

    private static string Player(int seat, WorldDescriptor world) =>
        seat < 0
            ? "the scenario"
            : world.Players.FirstOrDefault(player => player.Seat == seat)?.Name
                ?? $"player {seat + 1}";

    private static string PlayArea(int seat, WorldDescriptor world) =>
        seat < 0 ? "The villain play area" : $"{Player(seat, world)}'s play area";

    private static string Cause(GameEvent happened)
    {
        string verb = Words(happened.Verb);
        string trigger = Words(happened.Trigger);
        return (verb.Length, trigger.Length) switch
        {
            (0, 0) => "Engine resolution",
            (> 0, 0) => verb,
            (0, > 0) => trigger,
            _ => $"{verb} · {trigger}",
        };
    }

    private static string Words(string value, bool trimArea = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.StartsWith("k_", StringComparison.Ordinal)
            ? value[2..]
            : value;
        string text = trimArea && normalized.EndsWith("Area", StringComparison.Ordinal)
            ? normalized[..^"Area".Length]
            : normalized;
        var result = new StringBuilder(text.Length + 8);
        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];
            if (index > 0 && (current == '_'
                || char.IsUpper(current) && char.IsLower(text[index - 1])))
            {
                result.Append(' ');
            }

            if (current != '_')
            {
                result.Append(current);
            }
        }

        return result.ToString();
    }
}

/// <summary>Plans response-scoped cues without changing event order or game state.</summary>
public static class EventCuePlanner
{
    /// <summary>
    /// Keeps every event in history while folding a status card's companion
    /// attachment event into one visual beat. This is a client presentation choice.
    /// </summary>
    public static EventBatchPresentation Plan(
        IReadOnlyList<GameEvent> happened,
        WorldDescriptor world,
        Outcome previousOutcome)
    {
        ArgumentNullException.ThrowIfNull(happened);
        ArgumentNullException.ThrowIfNull(world);
        var history = EventPresenter.Present(happened, world).ToList();
        var cues = new List<EventPresentation>(history.Count + 1);
        for (int index = 0; index < history.Count; index++)
        {
            EventPresentation current = history[index];
            if (current.Motion == EventMotionKind.Status
                && StatusCardIds(happened[index]) is { Count: > 0 } statusCards)
            {
                var anchors = current.Anchors.ToList();
                while (index + 1 < history.Count
                    && CompanionCard(happened[index + 1]) is { } companionCard
                    && statusCards.Contains(companionCard))
                {
                    anchors.AddRange(history[++index].Anchors);
                }

                current = current with { Anchors = anchors.Distinct().ToArray() };
            }

            cues.Add(current);
        }

        if (previousOutcome == Outcome.Unfinished && world.Outcome != Outcome.Unfinished)
        {
            EventPresentation terminal = EventPresenter.Terminal(world.Outcome);
            history.Add(terminal);
            cues.Add(terminal);
        }

        return new EventBatchPresentation(history, cues);
    }

    private static HashSet<int> StatusCardIds(GameEvent status) =>
        status switch
        {
            CardsCreated created when IsStatus(created.Area) =>
                created.Cards.Select(card => card.Id).ToHashSet(),
            CardsMoved moved when IsStatus(moved.From) || IsStatus(moved.To) =>
                moved.Cards.Select(card => card.Card).ToHashSet(),
            _ => new HashSet<int>(),
        };

    private static int? CompanionCard(GameEvent companion) => companion switch
    {
        CardAttached value => value.Card,
        CardDetached value => value.Card,
        _ => null,
    };

    private static bool IsStatus(AreaRef area) =>
        string.Equals(area.Zone, "StatusArea", StringComparison.Ordinal);
}

/// <summary>An ordered, resettable chronology of semantic events across responses.</summary>
public sealed class EventChronology
{
    private const int MaximumEntries = 100;
    private readonly List<EventPresentation> entries = [];

    /// <summary>All entries in response and event order.</summary>
    public IReadOnlyList<EventPresentation> Entries => entries;

    /// <summary>Clears the prior game and records one response's events.</summary>
    public void Reset(IReadOnlyList<GameEvent> events, WorldDescriptor world)
    {
        entries.Clear();
        Append(events, world);
    }

    /// <summary>Clears the prior game and records already-presented entries.</summary>
    public void Reset(IEnumerable<EventPresentation> presented)
    {
        entries.Clear();
        Append(presented);
    }

    /// <summary>Appends one response's events without changing earlier entries.</summary>
    public void Append(IReadOnlyList<GameEvent> events, WorldDescriptor world) =>
        Append(EventPresenter.Present(events, world));

    /// <summary>Appends already-presented entries and retains the latest readable history.</summary>
    public void Append(IEnumerable<EventPresentation> presented)
    {
        ArgumentNullException.ThrowIfNull(presented);
        entries.AddRange(presented);
        if (entries.Count > MaximumEntries)
        {
            entries.RemoveRange(0, entries.Count - MaximumEntries);
        }
    }
}
