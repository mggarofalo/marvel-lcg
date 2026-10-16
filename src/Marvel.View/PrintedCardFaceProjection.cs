using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Canonical complete printed face data, shared by live cards and authorized occurrence references.</summary>
internal static class PrintedCardFaceProjection
{
    internal static CardFaceDescriptor From(ICardFacts facts, string id)
    {
        IReadOnlyDictionary<string, string> attributes = facts.Attributes(id);
        return new CardFaceDescriptor(id, facts.Title(id), facts.Subtitle(id), facts.Kind(id), new Dictionary<string, long>())
        {
            Traits = facts.PrintedTraits(id).ToArray(), Cost = attributes.GetValueOrDefault("Cost"),
            PrintedStats = PrintedFaceAttributes.PrintedStats(attributes),
            PrintedValues = CardPrintedValues.From(facts.PrintedStats(id)),
            Keywords = facts.Keywords(id).ToArray(), RulesText = facts.Text(id), RulesMarkup = facts.FormattedText(id),
            ArtFaceId = id,
        };
    }
}
