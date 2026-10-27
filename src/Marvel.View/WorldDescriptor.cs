using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>A client-safe snapshot of the table.</summary>
/// <param name="Players">Seats in seat order.</param>
/// <param name="Areas">Every runtime area in allocation order.</param>
/// <param name="GameAreas">The play-area groupings used by split scenarios.</param>
/// <param name="Outcome">How the game ended, or <see cref="Outcome.Unfinished"/>.</param>
public sealed record WorldDescriptor(
    IReadOnlyList<PlayerDescriptor> Players,
    IReadOnlyList<AreaDescriptor> Areas,
    IReadOnlyList<GameAreaDescriptor> GameAreas,
    Outcome Outcome)
{
    /// <summary>Typed public auxiliary piles, without concealed card identities or order.</summary>
    public IReadOnlyList<InvocationDeckDescriptor> InvocationDecks { get; init; } = [];

    /// <summary>Visibility-filtered independent encounter ownership and active-counter facts.</summary>
    public IReadOnlyList<EncounterGroupDescriptor> EncounterGroups { get; init; } = [];

    /// <summary>The public rule-owned cause distinguishing player-loss outcomes.</summary>
    public PlayerLossReason LossReason { get; init; }

    /// <summary>The authorized attack whose resolution is still open, independently of the current prompt.</summary>
    public ActiveAttackDescriptor? ActiveAttack { get; init; }

    /// <summary>Distinct seat roles for this response, selected by the authoritative host.</summary>
    public TableContextDescriptor? Table { get; init; }

    /// <summary>Compact public seat summaries suitable when one player area is expanded.</summary>
    public IReadOnlyList<PlayerSummaryDescriptor> PlayerSummaries { get; init; } = [];

    /// <summary>Explicit, visibility-reviewed card and prompt relationships.</summary>
    public IReadOnlyList<TableRelationshipDescriptor> Relationships { get; init; } = [];

    /// <summary>Printed references granted by authorized occurrences in this response, keyed by exact face identity.</summary>
    public IReadOnlyDictionary<string, BoardCardPresentation> EventFaces { get; init; } =
        new Dictionary<string, BoardCardPresentation>();
}
