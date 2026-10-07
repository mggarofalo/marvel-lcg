using Marvel.Rules.Events;
using Marvel.Rules.State;
using static Marvel.View.EventSubjectDescriptions;

namespace Marvel.View;

/// <summary>Explains why an authorized card face became visible.</summary>
internal static class EventFlipPresentation
{
    internal static string Summary(CardsFlipped flipped, WorldDescriptor world)
    {
        string cards = CardList(flipped.Cards, world, flipped);
        if (!flipped.FaceUp)
        {
            return $"Turned {cards} face down.";
        }

        if (string.Equals(flipped.Verb, "Boost", StringComparison.Ordinal))
        {
            return $"Turned {cards} face up for a boost.";
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
    internal static string CueSummary(CardsFlipped flipped, WorldDescriptor world)
    {
        string purpose = !flipped.FaceUp ? "Face down"
            : string.Equals(flipped.Verb, "Boost", StringComparison.Ordinal) ? "Boost" : "Reveal";
        return $"{purpose} · {CardList(flipped.Cards, world, flipped)}";
    }

}
