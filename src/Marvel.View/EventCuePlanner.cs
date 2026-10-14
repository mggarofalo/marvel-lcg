using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

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
        IReadOnlyList<GameEvent> originals = happened;
        HashSet<int> completed = happened.OfType<ActivityCompleted>().Select(activity => activity.Batch).ToHashSet();
        happened = happened.Where(item => item is ActivityCompleted || item.ActivityBatch is not { } batch
            || !completed.Contains(batch)).ToArray();
        var presented = EventPresenter.Present(happened, world).Select((entry, index) =>
            happened[index] is ActivityCompleted activity ? entry with
            {
                Details = EventPresenter.Present(originals.Where(component =>
                    component.ActivityBatch == activity.Batch && component is not ActivityCompleted).ToArray(), world),
            } : entry).ToArray();
        IReadOnlyList<EventPresentation> narrative =
            EventPresenter.PresentNarrative(happened, world);
        var history = narrative.Count == presented.Length
            ? presented.ToList()
            : narrative.ToList();
        List<EventPresentation> cues = GroupedCues(happened, presented);

        if (previousOutcome == Outcome.Unfinished && world.Outcome != Outcome.Unfinished)
        {
            EventPresentation terminal = EventPresenter.Terminal(world.Outcome, world.LossReason);
            history.Add(terminal);
            cues.Add(terminal);
        }

        var highlights = Highlight(history);
        return new EventBatchPresentation(history, cues, highlights);
    }

    private static List<EventPresentation> GroupedCues(IReadOnlyList<GameEvent> happened, EventPresentation[] presented)
    {
        var cues = new List<EventPresentation>(presented.Length + 1);
        var receipts = happened.OfType<AbilityWindowResult>()
            .Select(receipt => (receipt.Source, receipt.Occurrence, (Marvel.Rules.Timing.AbilityType?)receipt.Type, (int?)receipt.Ordinal)).ToHashSet();
        for (int index = 0; index < presented.Length; index++)
        {
            if (!ForcedAbilityResultPresentation.IsGroupedResult(happened[index], receipts))
                cues.Add(FoldStatusCompanions(happened, presented, ref index));
        }
        return cues;
    }

    private static EventPresentation FoldStatusCompanions(
        IReadOnlyList<GameEvent> happened, EventPresentation[] presented, ref int index)
    {
        EventPresentation current = presented[index];
        if (current.Motion != EventMotionKind.Status
            || StatusCardIds(happened[index]) is not { Count: > 0 } statusCards) return current;
        var anchors = current.Anchors.ToList();
        while (index + 1 < presented.Length
            && CompanionCard(happened[index + 1]) is { } companionCard
            && statusCards.Contains(companionCard))
        {
            EventPresentation companion = presented[++index];
            anchors.AddRange(companion.Anchors);
            current = WithStatusHost(current, companion);
        }
        return current with { Anchors = anchors.Distinct().ToArray() };
    }

    private static EventPresentation WithStatusHost(EventPresentation current, EventPresentation companion)
    {
        if (current.Activity is not { } status || companion.Activity is not { } attached) return current;
        return current with { Activity = status with
        { Subjects = status.Subjects.Concat(attached.Subjects.Where(subject =>
                subject.Role == ActivitySubjectRole.Target)).Distinct().ToArray() },
            Details = [.. current.Details, companion] };
    }

    /// <summary>Chooses a durable result using the same consequence priorities as the receipt.</summary>
    public static int SettledCueIndex(IReadOnlyList<EventPresentation> cues)
    {
        ArgumentNullException.ThrowIfNull(cues);
        IReadOnlyList<EventPresentation> highlights = Highlight(cues);
        EventPresentation? preferred = highlights.Count == 0 ? null : highlights[^1];
        for (int index = cues.Count - 1; index >= 0; index--)
            if (cues[index] == preferred) return index;
        return -1;
    }

    private static IReadOnlyList<EventPresentation> Highlight(
        IReadOnlyList<EventPresentation> cues)
    {
        var useful = cues.Where(cue => cue.Motion is
            EventMotionKind.Damage or EventMotionKind.Heal or EventMotionKind.Threat or EventMotionKind.Status
            or EventMotionKind.Attack or EventMotionKind.Defeat or EventMotionKind.Terminal
            or EventMotionKind.HandGain or EventMotionKind.Counter or EventMotionKind.DeckReturn).ToList();
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
            EventMotionKind.Attack or EventMotionKind.Defeat or EventMotionKind.Terminal).ToHashSet();
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
