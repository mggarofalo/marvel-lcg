using System.Text;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Builds visibility-safe card tiles for a board presentation.</summary>
internal static class BoardCardPresentationFactory
{
    internal static List<BoardCardPresentation> Present(IReadOnlyList<CardDescriptor> cards, string zone)
    {
        var presented = new List<BoardCardPresentation>();
        var concealed = new Dictionary<CardBack, int>();
        foreach (CardDescriptor card in cards)
        {
            if (card.Face is null && card.Id is null)
            {
                concealed[card.Back] = concealed.GetValueOrDefault(card.Back) + 1;
            }
            else
            {
                presented.Add(Present(card, zone));
            }
        }
        foreach ((CardBack back, int count) in concealed)
        {
            presented.Add(new BoardCardPresentation(null, count, true,
                $"{count} concealed {back.ToString().ToLowerInvariant()} {(count == 1 ? "card" : "cards")}",
                "Identity and order hidden", "CONCEALED PILE", $"{back.ToString().ToUpperInvariant()} BACK", [])
            { Back = back.ToString().ToUpperInvariant(), StageRole = StageRole(zone) });
        }
        return presented;
    }

    internal static BoardCardPresentation Present(CardDescriptor card, string zone)
    {
        if (card.Face is null)
        {
            return new BoardCardPresentation(card.Id, 1, true,
                $"Face-down {card.Back.ToString().ToLowerInvariant()} card", "Identity hidden",
                "CONCEALED CARD", Status(card, zone, null), [])
            { Back = card.Back.ToString().ToUpperInvariant(), StageRole = StageRole(zone) };
        }
        bool inPlay = IsInPlay(zone);
        BoardCardPresentation result = new(card.Id, 1, false, card.Face.Title, card.Face.Subtitle,
            Humanize(card.Face.Kind.ToString(), false).ToUpperInvariant(), Status(card, zone, card.Face.Kind),
            BoardCardFields.Present(card, inPlay).ToArray())
        {
            Back = card.Back.ToString().ToUpperInvariant(), FaceId = card.Face.ArtFaceId, StageRole = StageRole(zone),
            Traits = card.Face.Traits, Cost = card.Face.Cost,
            PrintedStats = card.Face.PrintedStats.Where(field => field.Key != "Class")
                .Select(field => new BoardFieldPresentation(field.Key, field.Value)).ToArray(),
            EffectiveValues = card.Face.EffectiveValues,
            Persistent = card.Persistent,
            PrintedMarks = BoardPrintedValueMarks.From(card.Face.PrintedValues),
            Classification = card.Face.PrintedStats.GetValueOrDefault("Class", string.Empty),
            Keywords = card.Face.Keywords, RulesText = card.Face.RulesText, RulesMarkup = card.Face.RulesMarkup,
        };
        return BoardCardLiveValues.Apply(result, card, inPlay);
    }

    private static BoardStageRole StageRole(string zone) => zone switch { "VillainArea" or "MainSchemesArea" => BoardStageRole.Current, "VillainDeck" or "MainSchemesDeck" => BoardStageRole.Upcoming, _ => BoardStageRole.None };
    private static string Status(CardDescriptor card, string zone, CardKind? kind)
    {
        bool inPlay = IsInPlay(zone);
        bool canExhaust = kind is null or CardKind.AlterEgo or CardKind.Hero or CardKind.Ally or CardKind.Support or CardKind.Upgrade;
        string status = inPlay && canExhaust ? card.Ready ? "READY" : "EXHAUSTED" : string.Empty;
        if (inPlay && !card.FaceUp) status += status.Length == 0 ? "FACE DOWN" : "  ·  FACE DOWN";
        return status;
    }

    internal static bool IsInPlay(string zone) => Enum.TryParse(zone, out DeckType deckType) && DeckTypes.IsInPlay(deckType);
    internal static string Humanize(string value, bool trimArea)
    {
        string text = trimArea && value.EndsWith("Area", StringComparison.Ordinal) ? value[..^"Area".Length] : value;
        var result = new StringBuilder(text.Length + 8);
        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];
            if (index > 0 && char.IsUpper(current) && (char.IsLower(text[index - 1]) || index + 1 < text.Length && char.IsLower(text[index + 1]))) result.Append(' ');
            result.Append(current);
        }
        return result.ToString();
    }
}
