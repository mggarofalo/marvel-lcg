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
    /// <summary>Distinct seat roles for this response, selected by the authoritative host.</summary>
    public TableContextDescriptor? Table { get; init; }

    /// <summary>Compact public seat summaries suitable when one player area is expanded.</summary>
    public IReadOnlyList<PlayerSummaryDescriptor> PlayerSummaries { get; init; } = [];

    /// <summary>Explicit, visibility-reviewed card and prompt relationships.</summary>
    public IReadOnlyList<TableRelationshipDescriptor> Relationships { get; init; } = [];
}
