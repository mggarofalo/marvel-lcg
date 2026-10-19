using Marvel.Protocol;

namespace Marvel.Client;

/// <summary>Assembles bounded research pages while preserving the host's separate identity admission.</summary>
public static class DeckResearchCatalog
{
    /// <summary>Reads the complete offline snapshot through either transport and rejects duplicate faces.</summary>
    public static async Task<DeckCatalog> LoadAsync(LocalGameClient client, DeckCatalog admitted,
        CancellationToken cancellationToken = default)
    {
        var cards = new Dictionary<string, DeckCard>(StringComparer.Ordinal);
        int? page = 0;
        while (page is { } current)
        {
            if (current > 1000) throw new InvalidDataException("The research catalog exceeds its page limit.");
            var result = await client.ReadDecksAsync(new("", []) { CatalogPage = current }, cancellationToken).ConfigureAwait(false);
            if (!result.Succeeded) throw new InvalidDataException(result.Error!.Message);
            var content = result.Decks!.Page!;
            foreach (var card in content.Cards)
                if (!cards.TryAdd(card.Face, card)) throw new InvalidDataException("The research catalog contains a duplicate face.");
            page = content.NextPage;
        }
        if (admitted.Cards.Any(card => !cards.ContainsKey(card.Face))) throw new InvalidDataException("Research catalog is missing admitted cards.");
        return new(admitted.Heroes, cards.Values.ToArray());
    }
}
