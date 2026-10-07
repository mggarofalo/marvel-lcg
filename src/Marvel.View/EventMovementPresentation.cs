using Marvel.Rules.Events;
using Marvel.Rules.State;
using static Marvel.View.EventSubjectDescriptions;

namespace Marvel.View;

/// <summary>Describes semantic card movement from engine-authored verbs.</summary>
internal static class EventMovementPresentation
{
    internal static string Summary(CardsMoved moved, WorldDescriptor world)
    {
        string cards = CardList(moved.Cards.Select(card => card.Card), world, moved);
        if (string.Equals(moved.Verb, "Return_To_Discard", StringComparison.Ordinal))
        {
            return ReturnedToDiscard(moved, cards, world);
        }

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

        if (IsReturnToHand(moved))
        {
            return $"{Player(moved.To.Owner, world)} returned {cards} to their hand"
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

    private static string ReturnedToDiscard(CardsMoved moved, string cards, WorldDescriptor world) =>
        moved.From == moved.To
            ? $"Now in {Area(moved.To, world)}: {cards}."
            : $"Returned {cards} to {Area(moved.To, world)}.";

    private static bool IsDraw(CardsMoved moved) =>
        ZoneIs(moved.From, "PlayerDeck")
        && ZoneIs(moved.To, "HandsArea")
        && string.Equals(moved.Verb, "Draw", StringComparison.Ordinal);

    internal static bool IsEffectHandGain(CardsMoved moved) =>
        IsAddToHand(moved) || IsReturnToHand(moved);

    private static bool IsReturnToHand(CardsMoved moved) =>
        ZoneIs(moved.To, "HandsArea")
        && string.Equals(moved.Verb, "Return", StringComparison.Ordinal);

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

}
