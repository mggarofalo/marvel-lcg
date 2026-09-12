using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One visibility-safe, human-readable entry in the game chronology.</summary>

/// <summary>Formats the closed semantic-event vocabulary without consulting engine state.</summary>
public static class EventPresenter
{
    private static readonly HashSet<string> HealthFields = new(
        ["health", "hitpoints", "hit_points"], StringComparer.Ordinal);
    private static readonly HashSet<string> DamageFields = new(
        ["damage", "k_damage"], StringComparer.Ordinal);

    /// <summary>Presents events in the engine-provided resolution order.</summary>
    public static IReadOnlyList<EventPresentation> Present(
        IReadOnlyList<GameEvent> events,
        WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(world);
        return events.Select(happened => Present(happened, world)).ToArray();
    }

    /// <summary>
    /// Presents one response as readable actions, combining adjacent pieces of
    /// the same card movement without changing the underlying event stream.
    /// </summary>
    public static IReadOnlyList<EventPresentation> PresentNarrative(
        IReadOnlyList<GameEvent> events,
        WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(world);
        var combined = new List<GameEvent>(events.Count);
        foreach (GameEvent happened in events)
        {
            if (happened is CardsMoved moved
                && combined.LastOrDefault() is CardsMoved prior
                && SameArea(prior.From, moved.From)
                && SameArea(prior.To, moved.To)
                && string.Equals(prior.Verb, moved.Verb, StringComparison.Ordinal)
                && string.Equals(prior.Trigger, moved.Trigger, StringComparison.Ordinal))
            {
                combined[^1] = prior with
                {
                    Cards = prior.Cards.Concat(moved.Cards).ToArray(),
                    Subjects = CombineSubjects(prior.Subjects, moved.Subjects),
                };
                continue;
            }

            combined.Add(happened);
        }

        return Present(combined, world);
    }

    private static Dictionary<int, string>? CombineSubjects(
        IReadOnlyDictionary<int, string>? first,
        IReadOnlyDictionary<int, string>? second)
    {
        if (first is null && second is null)
        {
            return null;
        }

        var combined = new Dictionary<int, string>();
        if (first is not null)
        {
            foreach ((int id, string subject) in first)
            {
                combined.Add(id, subject);
            }
        }
        if (second is not null)
        {
            foreach ((int id, string subject) in second)
            {
                combined[id] = subject;
            }
        }
        return combined;
    }

    /// <summary>Presents one event using only its authorized response snapshot.</summary>
    public static EventPresentation Present(GameEvent happened, WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(happened);
        ArgumentNullException.ThrowIfNull(world);

        (string summary, IReadOnlyList<int> anchors, EventMotionKind motion) = happened switch
        {
            CardsCreated created => (
                $"Created {Cards(created.Cards.Select(card => card.Id), world, created)} in {Area(created.Area, world)}.",
                created.Cards.Select(card => card.Id).ToArray(),
                IsStatus(created.Area) ? EventMotionKind.Status : EventMotionKind.Create),
            CardsMoved moved when string.Equals(
                moved.Verb, "Defeat", StringComparison.Ordinal) => (
                DefeatedSummary(moved, world),
                moved.Cards.Select(card => card.Card).ToArray(),
                EventMotionKind.Defeat),
            CardsMoved moved => (
                MovementSummary(moved, world),
                moved.Cards.Select(card => card.Card).ToArray(),
                IsStatus(moved.From) || IsStatus(moved.To)
                    ? EventMotionKind.Status
                    : EventMotionKind.Move),
            AreaReordered reordered => (
                $"Reordered {Area(reordered.Area, world)}.",
                Array.Empty<int>(),
                EventMotionKind.Move),
            CardFormChanged changed => (
                $"{Card(changed.Card, world, changed)} changed form.",
                [changed.Card],
                EventMotionKind.Flip),
            CardsFlipped flipped => (
                FlippedSummary(flipped, world),
                flipped.Cards.ToArray(),
                EventMotionKind.Flip),
            CardAttached attached => (
                $"Attached {Card(attached.Card, world, attached)} to {Card(attached.Host, world, attached)}.",
                [attached.Card, attached.Host],
                EventMotionKind.Move),
            CardDetached detached => (
                $"Detached {Card(detached.Card, world, detached)} from {Card(detached.Host, world, detached)}.",
                [detached.Card, detached.Host],
                EventMotionKind.Move),
            ControlChanged changed => (
                $"{Card(changed.Card, world, changed)} changed control from {Player(changed.From, world)} to {Player(changed.To, world)}.",
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

        if (HealthFields.Contains(field))
        {
            return HealthMotion(change.Value);
        }

        if (DamageFields.Contains(field))
        {
            return DamageMotion(change.Value);
        }

        if (field.StartsWith("c_", StringComparison.Ordinal)
            || field == EncounterDeck.AccelerationToken)
        {
            return EventMotionKind.Counter;
        }

        return EventMotionKind.State;
    }

    private static EventMotionKind HealthMotion(long change) =>
        change > 0 ? EventMotionKind.Heal : EventMotionKind.Damage;

    private static EventMotionKind DamageMotion(long change) =>
        change < 0 ? EventMotionKind.Heal : EventMotionKind.Damage;

    private static bool IsStatus(AreaRef area) =>
        string.Equals(area.Zone, "StatusArea", StringComparison.Ordinal);

    private static string FieldSummary(FieldSet set, WorldDescriptor world)
    {
        string subject = Card(set.Card, world, set);
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
        string cards = Cards(flipped.Cards, world, flipped);
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

    private static string Cards(
        IEnumerable<int> ids, WorldDescriptor world, GameEvent? happened = null)
    {
        string[] names = ids.Select(id => Card(id, world, happened)).ToArray();
        return names.Length switch
        {
            0 => "no cards",
            1 => names[0],
            2 => $"{names[0]} and {names[1]}",
            _ => $"{string.Join(", ", names[..^1])}, and {names[^1]}",
        };
    }

    private static string MovementSummary(CardsMoved moved, WorldDescriptor world)
    {
        string cards = Cards(moved.Cards.Select(card => card.Card), world, moved);
        if (IsDraw(moved))
        {
            string player = Player(moved.To.Owner, world);
            return moved.From.Owner == moved.To.Owner
                ? $"{player} drew {cards}."
                : $"{player} drew {cards} from {Possessive(moved.From.Owner, world)} player deck.";
        }

        if (IsAddToHand(moved))
        {
            return $"{Player(moved.To.Owner, world)} added {cards} to their hand"
                + $" from {Area(moved.From, world)}.";
        }

        if (IsHandDiscard(moved))
        {
            string player = Player(moved.From.Owner, world);
            return moved.From.Owner == moved.To.Owner
                ? $"{player} discarded {cards}."
                : $"{player} discarded {cards} to {Possessive(moved.To.Owner, world)} discard pile.";
        }

        if (string.Equals(moved.Verb, "Discard", StringComparison.Ordinal))
        {
            return $"{Player(moved.From.Owner, world)} discarded {cards} from "
                + $"{Area(moved.From, world)}.";
        }

        if (IsPlay(moved))
        {
            return $"{Player(moved.From.Owner, world)} played {cards}.";
        }

        return $"Moved {cards} from {Area(moved.From, world)} to {Area(moved.To, world)}.";
    }

    private static bool IsDraw(CardsMoved moved) =>
        ZoneIs(moved.From, "PlayerDeck")
        && ZoneIs(moved.To, "HandsArea")
        && string.Equals(moved.Verb, "Draw", StringComparison.Ordinal);

    private static bool IsAddToHand(CardsMoved moved) =>
        ZoneIs(moved.To, "HandsArea")
        && string.Equals(moved.Verb, "Add_To_Hand", StringComparison.Ordinal);

    private static bool IsHandDiscard(CardsMoved moved) =>
        ZoneIs(moved.From, "HandsArea") && ZoneIs(moved.To, "DiscardPile");

    private static bool IsPlay(CardsMoved moved) =>
        ZoneIs(moved.From, "HandsArea")
        && string.Equals(moved.Verb, "Play", StringComparison.Ordinal)
        && moved.From.Owner == moved.To.Owner;

    private static bool ZoneIs(AreaRef area, string zone) =>
        string.Equals(area.Zone, zone, StringComparison.Ordinal);

    private static bool SameArea(AreaRef left, AreaRef right) =>
        left.Owner == right.Owner
        && left.Host == right.Host
        && string.Equals(left.Zone, right.Zone, StringComparison.Ordinal);

    private static string Possessive(int seat, WorldDescriptor world) =>
        $"{Player(seat, world)}'s";

    private static string DefeatedSummary(CardsMoved moved, WorldDescriptor world)
    {
        string[] names = moved.Cards.Select(card =>
            DefeatedCard(card.Card, world, moved)).ToArray();
        string cards = names.Length switch
        {
            0 => "no cards",
            1 => names[0],
            2 => $"{names[0]} and {names[1]}",
            _ => $"{string.Join(", ", names[..^1])}, and {names[^1]}",
        };
        return $"{cards} {(moved.Cards.Count == 1 ? "was" : "were")} defeated.";
    }

    private static string DefeatedCard(int id, WorldDescriptor world, GameEvent happened)
    {
        if (happened.Subjects?.TryGetValue(id, out string? subject) == true)
        {
            return subject;
        }

        CardDescriptor? card = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(candidate => candidate.Id == id);
        if (card?.Face is { Kind: CardKind.EncounterVillain } face
            && face.PrintedStats.GetValueOrDefault("Stage") is { Length: > 0 } stage)
        {
            return $"{face.Title} stage {stage}";
        }
        return Card(id, world, happened);
    }

    private static string Card(
        int id, WorldDescriptor world, GameEvent? happened = null)
    {
        if (happened?.Subjects?.TryGetValue(id, out string? subject) == true)
        {
            return subject;
        }

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
            if (StartsWord(text, index, current))
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

    private static bool StartsWord(string text, int index, char current) =>
        index > 0
        && (current == '_' || char.IsUpper(current) && char.IsLower(text[index - 1]));
}
