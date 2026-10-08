using Marvel.Rules.Events;

namespace Marvel.View;

/// <summary>Describes a completed shuffle from its public receipt, never the hidden deck.</summary>
internal static class DeckReturnPresentation
{
    internal static string Summary(CardsShuffledIntoDeck shuffled, WorldDescriptor world)
    {
        string known = shuffled.PublicTitles.Count < shuffled.Count ? "including " : "";
        string names = shuffled.PublicTitles.Count == 0 ? ""
            : $" ({known}{string.Join(", ", shuffled.PublicTitles)})";
        return $"{EventSubjectDescriptions.Player(shuffled.Player, world)} shuffled "
            + $"{shuffled.Count} card{(shuffled.Count == 1 ? "" : "s")}{names} into their deck.";
    }
}
