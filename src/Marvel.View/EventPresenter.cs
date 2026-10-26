using System.Globalization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

using static Marvel.View.EventSubjectDescriptions;

namespace Marvel.View;

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
        return ForcedAbilityResultPresentation.Attach(events,
            events.Select(happened => Present(happened, world)).ToArray());
    }

    /// <summary>
    /// Presents engine-authored instruction receipts, retaining every unrelated event in order.
    /// </summary>
    public static IReadOnlyList<EventPresentation> PresentNarrative(
        IReadOnlyList<GameEvent> events,
        WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(world);
        HashSet<int> completed = events.OfType<ActivityCompleted>().Select(activity => activity.Batch).ToHashSet();
        return Present(events.Where(happened => happened is ActivityCompleted
            || happened.ActivityBatch is not { } batch || !completed.Contains(batch)).ToArray(), world);
    }

    /// <summary>Presents one event using only its authorized response snapshot.</summary>
    public static EventPresentation Present(GameEvent happened, WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(happened);
        ArgumentNullException.ThrowIfNull(world);

        var (summary, anchors, motion) = Describe(happened, world);

        EventActivityDescriptor activityDescriptor = EventActivityProjection.From(happened, world, anchors);
        return EventRelationshipProjection.WithSubjects(new EventPresentation(summary, Cause(happened), anchors, motion)
        {
            Activity = activityDescriptor,
            Intention = happened.Cause is { } causal ? causal.Kind switch
            {
                EventCauseKind.Scheme => $"{causal.SourceTitle} schemes against {causal.TargetTitle}",
                EventCauseKind.Thwart when causal.Target == -1 => $"{causal.SourceTitle} thwarts",
                EventCauseKind.Thwart => $"{causal.SourceTitle} thwarts {causal.TargetTitle}",
                _ => null,
            } : null,
            ForcedAbilityNotification = happened is AbilityWindowResult receipt
                ? new ForcedAbilityNotification(receipt.Source,
                    $"{Card(receipt.Source, world, receipt)} — {PromptPresentation.Words(receipt.Type.ToString())}",
                    summary, receipt.Player, receipt.Occurrence, receipt.Completed)
                { SourceCard = activityDescriptor.Subjects.FirstOrDefault(subject => subject.Card == receipt.Source)?.Reference } : null,
            CueSummary = happened switch
            {
                FieldSet set => EventFieldPresentation.CueSummary(set, world),
                CardsFlipped flipped => EventFlipPresentation.CueSummary(flipped, world),
                _ => null,
            },
        }, happened);
    }

    private static (string Summary, IReadOnlyList<int> Anchors, EventMotionKind Motion) Describe(
        GameEvent happened, WorldDescriptor world) =>
        happened switch
        {
            AttackBoundary boundary => DescribeAttack(boundary, world),
            AbilityWindowResult result => (result.Description + (result.Completed
                ? ". Ability resolved." : ". Resolution awaits a decision."), [result.Source], EventMotionKind.State),
            PhaseStarted phase => ($"Round {phase.Round}: {phase.Phase} phase.", [], EventMotionKind.State),
            ActivityCompleted activity => (
                CompletedActivityPresentation.Summary(activity, world),
                activity.Participants.SelectMany(participant => participant.Cards).ToArray(),
                CompletedActivityPresentation.Motion(activity)),
            CardsCreated created => (
                $"Created {CardList(created.Cards.Select(card => card.Id), world, created)} in {Area(created.Area, world)}.",
                created.Cards.Select(card => card.Id).ToArray(),
                IsStatus(created.Area) ? EventMotionKind.Status : EventMotionKind.Create),
            CardsMoved moved when string.Equals(
                moved.Verb, "Defeat", StringComparison.Ordinal) => (
                DefeatedSummary(moved, world),
                moved.Cards.Select(card => card.Card).ToArray(),
                EventMotionKind.Defeat),
            CardsMoved moved => (
                EventMovementPresentation.Summary(moved, world),
                moved.Cards.Select(card => card.Card).ToArray(),
                IsStatus(moved.From) || IsStatus(moved.To)
                    ? EventMotionKind.Status
                    : EventMovementPresentation.IsEffectHandGain(moved)
                        ? EventMotionKind.HandGain
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
                EventFlipPresentation.Summary(flipped, world),
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
                EventFieldPresentation.Summary(set, world),
                [set.Card],
                EventFieldPresentation.Motion(set)),
            BoostResolved boost => (
                $"{Card(boost.Card, world, boost)} added {boost.Icons} boost icon{(boost.Icons == 1 ? "" : "s")}; "
                + $"{Card(boost.Enemy, world, boost)}'s {(boost.Attacking ? "ATK" : "SCH")} is now {boost.Strength}.",
                [boost.Card, boost.Enemy], EventMotionKind.State),
            AttackCompleted completed => (
                AttackCompletionPresentation.Summary(completed, world),
                AttackCompletionPresentation.Anchors(completed),
                EventMotionKind.Attack),
            CardsShuffledIntoDeck shuffled => (
                DeckReturnPresentation.Summary(shuffled, world), [], EventMotionKind.DeckReturn),
            WhenRevealedCanceled canceled => (
                canceled.Source is { } source
                    ? $"{Card(source, world, canceled)} canceled {Card(canceled.Card, world, canceled)}'s When Revealed effects."
                    : $"{Card(canceled.Card, world, canceled)}'s When Revealed effects were canceled.",
                [canceled.Card, .. canceled.Source is { } known ? new[] { known } : Array.Empty<int>()], EventMotionKind.State),
            _ => throw new InvalidOperationException(
                $"event kind {happened.GetType().Name} has no presentation"),
        };

    private static (string Summary, IReadOnlyList<int> Anchors, EventMotionKind Motion) DescribeAttack(
        AttackBoundary boundary, WorldDescriptor world)
    {
        if (boundary.Target < 0)
            return ($"{Card(boundary.Source, world, boundary)}"
                + (boundary.Started ? " · Attack begins without an attacked enemy." : " · Attack resolved without an attacked enemy."),
                [boundary.Source], EventMotionKind.Attack);
        return ($"{Card(boundary.Source, world, boundary)} → {Card(boundary.Target, world, boundary)}"
            + (boundary.Started ? boundary.Resumed ? " · Attack resumes." : " · Attack begins." : " · Attack resolved."),
            [boundary.Source, boundary.Target], EventMotionKind.Attack);
    }

    /// <summary>Describes a newly reached terminal state without inventing a game event.</summary>
    public static EventPresentation Terminal(Outcome outcome,
        PlayerLossReason lossReason = PlayerLossReason.Unspecified) => outcome switch
    {
        Outcome.VillainWins => new(
            OutcomeDescription.Result(outcome), "Game outcome", [], EventMotionKind.Terminal),
        Outcome.PlayersLose => new(
            lossReason == PlayerLossReason.Unspecified ? "The players lost the game."
                : OutcomeDescription.Result(outcome, lossReason), "Game outcome", [], EventMotionKind.Terminal),
        Outcome.PlayersWin => new(
            "The players won the game.", "Game outcome", [], EventMotionKind.Terminal),
        _ => throw new ArgumentOutOfRangeException(
            nameof(outcome), outcome, "unfinished games have no terminal presentation"),
    };

    private static bool IsStatus(AreaRef area) =>
        string.Equals(area.Zone, "StatusArea", StringComparison.Ordinal);

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

}
