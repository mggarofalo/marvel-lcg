using Marvel.Protocol;

namespace Marvel.Client;

/// <summary>Read-only deck queries with correlation and response-shape validation.</summary>
internal static class DeckClient
{
    internal static async ValueTask<ClientDeckResult> QueryAsync(LocalGameClient client, DeckQuery? draft,
        CancellationToken cancellationToken)
    {
        string id = client.NextRequestId("decks");
        try
        {
            var response = await client.transport.ExchangeAsync(EngineRequest.ReadDecks(id, draft), cancellationToken)
                .ConfigureAwait(false);
            var envelope = ClientResponseValidation.EnvelopeError(response, id, "");
            if (envelope is not null) return new(null, envelope);
            if (response.Error is not null)
                return ClientResponseValidation.Complete(response.Error)
                    ? new(null, ClientResponseValidation.Error(response.Error.Code, response.Error.Message))
                    : new(null, new("invalid_response", "The game service returned an incomplete error."));
            if (!PublicOnly(response) || !Complete(response.Decks, draft))
                return new(null, new("invalid_response", "The game service returned incomplete deck data."));
            return new(response.Decks, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return new(null, new("transport_unavailable", "Deck data could not be loaded. Your draft has not changed."));
        }
    }

    private static bool PublicOnly(EngineResponse response) => response is
    {
        Capability: null, Prompt: null, World: null, Setup: null, History: null,
        Invitations: null, Revision: 0, Events.Count: 0,
    };

    private static bool Complete(DeckResponse? response, DeckQuery? query)
    {
        if (query?.CatalogPage is not { } page) return response?.Page is null && CompleteDraft(response, query is not null);
        return response is { Catalog: null, Assessment: null, Page.Cards.Count: <= 200 }
            && (response.Page.NextPage is null || response.Page.NextPage == page + 1)
            && response.Page.Cards.All(CompleteCard);
    }

    private static bool CompleteDraft(DeckResponse? response, bool assessment) => assessment
        ? response is { Catalog: null, Assessment.Diagnostics: not null }
            && response.Assessment.Diagnostics.All(item => item is not null
                && !string.IsNullOrWhiteSpace(item.Code) && item.Face is not null && item.Message is not null)
        : response is { Assessment: null, Catalog: not null } && CompleteCatalog(response.Catalog);

    private static bool CompleteCatalog(DeckCatalog catalog)
    {
        if (catalog.Heroes is not { Count: > 0 } || catalog.Cards is not { Count: > 0 }) return false;
        if (!catalog.Cards.All(CompleteCard) || !catalog.Heroes.All(CompleteHero)) return false;
        var faces = catalog.Cards.Select(card => card.Face).ToHashSet(StringComparer.Ordinal);
        return faces.Count == catalog.Cards.Count
            && catalog.Heroes.Select(hero => hero.Key).Distinct(StringComparer.Ordinal).Count() == catalog.Heroes.Count
            && catalog.Heroes.All(hero => hero.Signature.Concat(hero.Starter).All(faces.Contains));
    }

    private static bool CompleteCard(DeckCard card) => card is not null
        && !string.IsNullOrWhiteSpace(card.Face) && card.Title is not null && card.Kind is not null
        && card.Aspect is not null && card.Traits is not null && card.Text is not null
        && CompleteCardPresentation(card);

    private static bool CompleteCardPresentation(DeckCard card) => card.SearchFields is not null
        && card.SearchFields.Values.All(value => value is not null)
        && DeckPreviewValidation.Complete(card.Preview, card.Face);

    private static bool CompleteHero(DeckHero hero) => hero is not null
        && !string.IsNullOrWhiteSpace(hero.Key) && hero.Name is not null
        && hero.IdentityFaces is { Count: > 0 } && hero.IdentityFaces.All(face => !string.IsNullOrWhiteSpace(face))
        && hero.Signature is not null && hero.Starter is not null
        && CompleteIdentityPreviews(hero);

    private static bool CompleteIdentityPreviews(DeckHero hero) => hero.Previews is not null
        && hero.Previews.Count == hero.IdentityFaces.Count
        && hero.Previews.Select((face, index) => DeckPreviewValidation.Complete(face, hero.IdentityFaces[index])).All(valid => valid);
}
