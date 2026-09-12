using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One visibility-safe, human-readable entry in the game chronology.</summary>

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
