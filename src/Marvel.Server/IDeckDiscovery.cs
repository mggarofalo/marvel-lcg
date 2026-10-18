using Marvel.Protocol;
using Marvel.Rules.Setup;

namespace Marvel.Server;

/// <summary>Public construction data and engine-owned draft assessment.</summary>
public interface IDeckDiscovery
{
    /// <summary>The exact admitted player-card pool.</summary>
    DeckCatalog DiscoverDecks();

    /// <summary>Assess a draft without creating or changing a game.</summary>
    DeckAssessment AssessDeck(string hero, IReadOnlyList<string> draft);
}
