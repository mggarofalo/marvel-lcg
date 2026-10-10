using Marvel.Rules.Events;

namespace Marvel.Client;

/// <summary>Validates the closed semantic-event payloads before client state accepts them.</summary>
internal static class ClientEventValidation
{
    internal static bool Complete(GameEvent happened) =>
        happened is not null && happened.Trigger is not null && happened.Verb is not null
        && CompleteEventPayload(happened);

    private static bool CompleteEventPayload(GameEvent happened) => happened switch
            {
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
                CardsShuffledIntoDeck shuffled => CompleteShuffleReceipt(shuffled),
                WhenRevealedCanceled canceled => canceled.Card >= 0
                    && (canceled.Source is null or >= 0),
                _ => false,
            };

    private static bool CompleteShuffleReceipt(CardsShuffledIntoDeck shuffled) =>
        shuffled.Player >= 0 && shuffled.Count >= 0 && shuffled.PublicTitles is not null
        && shuffled.PublicTitles.Count <= shuffled.Count
        && shuffled.PublicTitles.All(title => !string.IsNullOrWhiteSpace(title));

    internal static bool Complete(AreaRef area) =>
        area.Zone is not null && area.Id is not null;

}
