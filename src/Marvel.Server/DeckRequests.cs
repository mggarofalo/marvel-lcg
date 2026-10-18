using Marvel.Protocol;
using static Marvel.Server.EngineHostLifecycle;

namespace Marvel.Server;

/// <summary>Bounded public deck queries, independent of session capabilities and game state.</summary>
internal static class DeckRequests
{
    internal static EngineResponse Handle(EngineRequest request, IGameFactory factory)
    {
        if (request is not { GameId: "", Entry: null, Capability: null, Game: null, Decision: null,
            Viewer: null, ExpectedRevision: null, Cursor: null, Order: null })
            return Failed(request, "invalid_request", "deck queries accept only a request id and optional draft");
        if (factory is not IDeckDiscovery discovery)
            return Failed(request, "decks_unavailable", "deck construction is unavailable from this host");
        DeckResponse result;
        if (request.Deck is null) result = new(discovery.DiscoverDecks(), null);
        else if (request.Deck.CatalogPage is { } page)
        {
            if (!ResearchPageWithinBounds(request.Deck, page))
                return Failed(request, "invalid_request", "research pages require an empty draft and a bounded page number");
            result = new(null, null) { Page = discovery.BrowseCards(page) };
        }
        else
        {
            var draft = request.Deck;
            // Product input bounds protect pregame work; these are not deckbuilding rules.
            if (!WithinBounds(draft))
                return Failed(request, "invalid_request", "deck draft exceeds the supported input bounds");
            if (!discovery.DiscoverDecks().Heroes.Any(hero => hero.Key == draft.Hero))
                return Failed(request, "invalid_request", "choose a hero offered by this host");
            var assessment = discovery.AssessDeck(draft.Hero, draft.Cards);
            result = new(null, new(assessment.Size, assessment.Diagnostics)
            {
                CanExtend = assessment.CanExtend,
                Quantities = draft.IncludeQuantities ? discovery.DeckQuantities(draft.Hero, draft.Cards) : new Dictionary<string, int[]>(),
            });
        }
        return new(EngineProtocol.Version, request.RequestId, "", null, null, []) { Decks = result };
    }

    private static bool ResearchPageWithinBounds(DeckQuery query, int page)
    {
        return page is >= 0 and <= 1000 && query.Hero == "" && query.Cards is { Count: 0 } && !query.IncludeQuantities;
    }

    private static bool WithinBounds(DeckQuery draft) => draft.Hero is { Length: <= 80 }
        && draft.Cards is { Count: <= 500 } && draft.Cards.All(face => face is { Length: <= 80 });
}
