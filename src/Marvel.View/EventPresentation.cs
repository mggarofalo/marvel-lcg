using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
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

    /// <summary>A named card was defeated and left play.</summary>
    Defeat,

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
    IReadOnlyList<EventPresentation> Cues,
    IReadOnlyList<EventPresentation> Highlights);

/// <summary>Authorized engine facts for one completed player action.</summary>
public sealed record ActionHistoryFacts(
    int Cursor,
    string Actor,
    string Role,
    string Phase,
    string? Verb,
    string Action,
    int? Subject,
    IReadOnlyList<int> ResourceGeneratorIds,
    IReadOnlyList<string> ResourceGenerators,
    Outcome? Outcome = null);

/// <summary>Formats completed actions without exposing raw event diagnostics.</summary>
public static class ActionHistoryPresenter
{
    /// <summary>Returns one player-facing sentence for a completed history unit.</summary>
    public static string Present(ActionHistoryFacts action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(action.Actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(action.Action);
        ArgumentNullException.ThrowIfNull(action.ResourceGeneratorIds);
        ArgumentNullException.ThrowIfNull(action.ResourceGenerators);

        string summary;
        if (string.Equals(action.Verb, CardPlay.Verb, StringComparison.Ordinal))
        {
            string payment = action.ResourceGenerators.Count == 0
                ? string.Empty
                : $", generating resources from {Names(action.ResourceGenerators)}";
            summary = $"{action.Actor} played {action.Action}{payment}.";
        }
        else if (string.Equals(action.Verb, Game.ChangeForm, StringComparison.Ordinal))
        {
            summary = $"{action.Actor} changed form.";
        }
        else if (string.Equals(action.Verb, Game.EndPhaseVerb, StringComparison.Ordinal)
            && string.Equals(action.Phase, "PlayerTurn", StringComparison.Ordinal))
        {
            summary = $"{action.Actor} ended their turn.";
        }
        else if (string.Equals(action.Verb, BasicPowers.AttackVerb, StringComparison.Ordinal))
        {
            summary = string.Equals(action.Actor, action.Action, StringComparison.Ordinal)
                ? $"{action.Actor} attacked."
                : $"{action.Actor} attacked with {action.Action}.";
        }
        else if (string.Equals(action.Verb, BasicPowers.ThwartVerb, StringComparison.Ordinal))
        {
            summary = string.Equals(action.Actor, action.Action, StringComparison.Ordinal)
                ? $"{action.Actor} thwarted."
                : $"{action.Actor} thwarted with {action.Action}.";
        }
        else if (string.Equals(action.Verb, BasicPowers.RecoverVerb, StringComparison.Ordinal))
        {
            summary = $"{action.Actor} recovered.";
        }
        else
        {
            string choice = action.Action.Trim().TrimEnd('.');
            string phase = Words(action.Phase);
            string phaseName = phase.EndsWith(" phase", StringComparison.Ordinal)
                ? phase
                : $"{phase} phase";
            summary = action.Role == "phase_step"
                || !string.Equals(action.Phase, "PlayerTurn", StringComparison.Ordinal)
                ? $"{action.Actor} resolved {choice} during the {phaseName}."
                : $"{action.Actor} used {choice}.";
        }

        return action.Outcome is { } outcome
            ? $"{summary} {EventPresenter.Terminal(outcome).Summary}"
            : summary;
    }

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

        GameEvent[] discards = events
            .OfType<CardsMoved>()
            .Where(moved => string.Equals(moved.Verb, "Discard", StringComparison.Ordinal))
            .Select(moved => moved with
            {
                Cards = moved.Cards.Where(card => !mechanics.Contains(card.Card)).ToArray(),
            })
            .Where(moved => moved.Cards.Count > 0)
            .ToArray();
        return EventPresenter.PresentNarrative(discards, world)
            .Select(entry => entry.Summary)
            .ToArray();
    }

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
                };
                continue;
            }

            combined.Add(happened);
        }

        return Present(combined, world);
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

    private static string MovementSummary(CardsMoved moved, WorldDescriptor world)
    {
        string cards = Cards(moved.Cards.Select(card => card.Card), world);
        if (ZoneIs(moved.From, "PlayerDeck")
            && ZoneIs(moved.To, "HandsArea")
            && string.Equals(moved.Verb, "Draw", StringComparison.Ordinal))
        {
            string player = Player(moved.To.Owner, world);
            return moved.From.Owner == moved.To.Owner
                ? $"{player} drew {cards}."
                : $"{player} drew {cards} from {Possessive(moved.From.Owner, world)} player deck.";
        }

        if (ZoneIs(moved.To, "HandsArea")
            && string.Equals(moved.Verb, "Add_To_Hand", StringComparison.Ordinal))
        {
            return $"{Player(moved.To.Owner, world)} added {cards} to their hand"
                + $" from {Area(moved.From, world)}.";
        }

        if (ZoneIs(moved.From, "HandsArea") && ZoneIs(moved.To, "DiscardPile"))
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

        if (ZoneIs(moved.From, "HandsArea")
            && string.Equals(moved.Verb, "Play", StringComparison.Ordinal)
            && moved.From.Owner == moved.To.Owner)
        {
            return $"{Player(moved.From.Owner, world)} played {cards}.";
        }

        return $"Moved {cards} from {Area(moved.From, world)} to {Area(moved.To, world)}.";
    }

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
            DefeatedCard(card.Card, world)).ToArray();
        string cards = names.Length switch
        {
            0 => "no cards",
            1 => names[0],
            2 => $"{names[0]} and {names[1]}",
            _ => $"{string.Join(", ", names[..^1])}, and {names[^1]}",
        };
        return $"{cards} {(moved.Cards.Count == 1 ? "was" : "were")} defeated.";
    }

    private static string DefeatedCard(int id, WorldDescriptor world)
    {
        CardDescriptor? card = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(candidate => candidate.Id == id);
        if (card?.Face is { Kind: CardKind.EncounterVillain } face
            && face.PrintedStats.GetValueOrDefault("Stage") is { Length: > 0 } stage)
        {
            return $"{face.Title} stage {stage}";
        }
        return Card(id, world);
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
        var presented = EventPresenter.Present(happened, world);
        IReadOnlyList<EventPresentation> narrative =
            EventPresenter.PresentNarrative(happened, world);
        var history = narrative.Count == presented.Count
            ? presented.ToList()
            : narrative.ToList();
        var cues = new List<EventPresentation>(presented.Count + 1);
        for (int index = 0; index < presented.Count; index++)
        {
            EventPresentation current = presented[index];
            if (current.Motion == EventMotionKind.Status
                && StatusCardIds(happened[index]) is { Count: > 0 } statusCards)
            {
                var anchors = current.Anchors.ToList();
                while (index + 1 < presented.Count
                    && CompanionCard(happened[index + 1]) is { } companionCard
                    && statusCards.Contains(companionCard))
                {
                    anchors.AddRange(presented[++index].Anchors);
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

        var highlights = Highlight(history);
        return new EventBatchPresentation(history, cues, highlights);
    }

    private static IReadOnlyList<EventPresentation> Highlight(
        IReadOnlyList<EventPresentation> cues)
    {
        var useful = cues.Where(cue => cue.Motion is
            EventMotionKind.Damage or EventMotionKind.Heal or EventMotionKind.Status
            or EventMotionKind.Defeat or EventMotionKind.Terminal).ToList();
        if (useful.Count == 0)
        {
            // A completed action still deserves acknowledgement when it did
            // not cause damage or a status change. Keep the most recent beats
            // concise while replacing a stale result from an earlier action.
            useful.AddRange(cues.TakeLast(4));
        }
        if (useful.Count <= 4)
        {
            return useful;
        }

        var essential = useful.Where(cue => cue.Motion is
            EventMotionKind.Defeat or EventMotionKind.Terminal).ToHashSet();
        foreach (var cue in useful.AsEnumerable().Reverse())
        {
            if (essential.Count >= 4)
            {
                break;
            }
            essential.Add(cue);
        }
        return useful.Where(essential.Contains).ToArray();
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
        Append(EventPresenter.PresentNarrative(events, world));

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
