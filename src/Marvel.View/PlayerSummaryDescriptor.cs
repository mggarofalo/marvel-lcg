namespace Marvel.View;

/// <summary>Compact public state for a seat that is not expanded into a private tableau.</summary>
/// <remarks>
/// All card references are response-scoped public object ids. Concealed cards, hand
/// contents, deck order, and private affordances are intentionally absent.
/// </remarks>
public sealed record PlayerSummaryDescriptor(
    int Seat,
    int? Identity,
    string? Form,
    long? Health,
    IReadOnlyList<int> EngagedEnemies,
    IReadOnlyList<string> Statuses,
    IReadOnlyList<int> OfferedDefenders);
