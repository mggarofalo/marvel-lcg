using Marvel.Rules.Events;

namespace Marvel.Client;

/// <summary>Validates the closed semantic-event payloads before client state accepts them.</summary>
internal static class ClientEventValidation
{
    internal static bool Complete(GameEvent happened) =>
        happened is not null && happened.Trigger is not null && happened.Verb is not null
        && (happened.Cause is null || CompleteCause(happened.Cause))
        && CompleteEventPayload(happened);

    // Engine wire choice: a thwart that resolves without a scheme spells its absent
    // target -1; a scheme activation always names the player it schemes against.
    private static bool CompleteCause(EventCause cause) =>
        cause is { Source: >= 0, SourceTitle: not null, TargetTitle: not null }
            and ({ Kind: EventCauseKind.Thwart, Target: >= -1 } or { Kind: EventCauseKind.Scheme, Target: >= 0 });

    private static bool CompleteEventPayload(GameEvent happened) => happened switch
            {
                ActivityCompleted activity => CompleteActivity(activity),
                AbilityWindowResult result => CompleteWindow(result),
                PhaseStarted phase => CompletePhase(phase),
                CardsCreated created => Complete(created.Area)
                    && created.Cards is not null
                    && created.Cards.All(card => card.Card is not null),
                CardsMoved moved => Complete(moved.From)
                    && Complete(moved.To)
                    && moved.Cards is not null,
                AreaReordered reordered => Complete(reordered.Area)
                    && reordered.Order is not null,
                CardFormChanged changed => changed.From is not null
                    && changed.To is not null,
                CardsFlipped flipped => flipped.Cards is not null,
                CardAttached => true,
                CardDetached => true,
                ControlChanged => true,
                PlayAreaJoined => true,
                PlayAreaDetached => true,
                FieldSet set => set.Field is not null,
                BoostResolved boost => boost.Card >= 0 && boost.Enemy >= 0 && boost.Icons >= 0,
                AttackCompleted => true,
                AttackBoundary boundary => CompleteBoundary(boundary),
                CardsShuffledIntoDeck shuffled => CompleteShuffleReceipt(shuffled),
                WhenRevealedCanceled canceled => canceled.Card >= 0
                    && (canceled.Source is null or >= 0),
                _ => false,
            };

    // Engine wire choice: an attack that begins before it has a target (Giant Stomp
    // chooses its enemy in a later sentence) spells its absent target -1.
    private static bool CompleteBoundary(AttackBoundary boundary) => boundary.Identity is not null
        && boundary.Source >= 0 && boundary.Target >= -1 && boundary.AttackIdentity == boundary.Identity;

    private static bool CompleteWindow(AbilityWindowResult result) =>
        result.Source >= 0 && result.Player >= -1 && result.Occurrence >= 0
        && Marvel.Rules.Timing.AbilityTypes.IsMandatory(result.Type)
        && !string.IsNullOrWhiteSpace(result.Description);

    private static bool CompleteActivity(ActivityCompleted activity) =>
        activity.Batch >= 0 && activity.Participants is { Count: > 0 }
        && activity.Participants.All(participant => participant is not null && participant.Player >= -1
            && participant.Count > 0 && participant.Cards is not null
            && participant.Cards.Count <= participant.Count && participant.Cards.All(card => card >= 0))
        && activity.Verb is "Draw" or "Discard" or "Ready";

    private static bool CompletePhase(PhaseStarted phase) =>
        phase.Round is >= 0 && phase.Phase is "Player" or "Villain";

    private static bool CompleteShuffleReceipt(CardsShuffledIntoDeck shuffled) =>
        shuffled.Player >= 0 && shuffled.Count >= 0 && shuffled.PublicTitles is not null
        && shuffled.PublicTitles.Count <= shuffled.Count
        && shuffled.PublicTitles.All(title => !string.IsNullOrWhiteSpace(title));

    internal static bool Complete(AreaRef area) =>
        area.Zone is not null && area.Id is not null;

}
