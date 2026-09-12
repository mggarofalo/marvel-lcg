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
    Outcome Outcome);
