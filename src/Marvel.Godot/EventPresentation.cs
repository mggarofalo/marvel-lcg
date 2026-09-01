using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>One visibility-safe, human-readable entry in the game chronology.</summary>
public sealed record EventPresentation(
    string Summary,
    string Cause,
    IReadOnlyList<int> Anchors);

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

        (string summary, IReadOnlyList<int> anchors) = happened switch
        {
            CardsCreated created => (
                $"Created {Cards(created.Cards.Select(card => card.Id), world)} in {Area(created.Area, world)}.",
                created.Cards.Select(card => card.Id).ToArray()),
            CardsMoved moved => (
                $"Moved {Cards(moved.Cards.Select(card => card.Card), world)} from {Area(moved.From, world)} to {Area(moved.To, world)}.",
                moved.Cards.Select(card => card.Card).ToArray()),
            AreaReordered reordered => (
                $"Reordered {Area(reordered.Area, world)}.",
                Array.Empty<int>()),
            CardFormChanged changed => (
                $"{Card(changed.Card, world)} changed form.",
                [changed.Card]),
            CardsFlipped flipped => (
                $"Turned {Cards(flipped.Cards, world)} face {(flipped.FaceUp ? "up" : "down")}.",
                flipped.Cards.ToArray()),
            CardAttached attached => (
                $"Attached {Card(attached.Card, world)} to {Card(attached.Host, world)}.",
                [attached.Card, attached.Host]),
            CardDetached detached => (
                $"Detached {Card(detached.Card, world)} from {Card(detached.Host, world)}.",
                [detached.Card, detached.Host]),
            ControlChanged changed => (
                $"{Card(changed.Card, world)} changed control from {Player(changed.From, world)} to {Player(changed.To, world)}.",
                [changed.Card]),
            PlayAreaJoined joined => (
                $"{PlayArea(joined.PlayArea, world)} joined game area {joined.GameArea.ToString(CultureInfo.InvariantCulture)}.",
                Array.Empty<int>()),
            PlayAreaDetached detached => (
                $"{PlayArea(detached.PlayArea, world)} left game area {detached.GameArea.ToString(CultureInfo.InvariantCulture)}.",
                Array.Empty<int>()),
            FieldSet set => (
                FieldSummary(set, world),
                [set.Card]),
            _ => throw new InvalidOperationException(
                $"event kind {happened.GetType().Name} has no presentation"),
        };

        return new EventPresentation(summary, Cause(happened), anchors);
    }

    private static string FieldSummary(FieldSet set, WorldDescriptor world)
    {
        string subject = Card(set.Card, world);
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

        string text = trimArea && value.EndsWith("Area", StringComparison.Ordinal)
            ? value[..^"Area".Length]
            : value;
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

/// <summary>An ordered, resettable chronology of semantic events across responses.</summary>
public sealed class EventChronology
{
    private readonly List<EventPresentation> entries = [];

    /// <summary>All entries in response and event order.</summary>
    public IReadOnlyList<EventPresentation> Entries => entries;

    /// <summary>Clears the prior game and records one response's events.</summary>
    public void Reset(IReadOnlyList<GameEvent> events, WorldDescriptor world)
    {
        entries.Clear();
        Append(events, world);
    }

    /// <summary>Appends one response's events without changing earlier entries.</summary>
    public void Append(IReadOnlyList<GameEvent> events, WorldDescriptor world) =>
        entries.AddRange(EventPresenter.Present(events, world));
}
