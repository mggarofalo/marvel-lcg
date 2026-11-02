namespace Marvel.Rules.Play;

/// <summary>A Kang (II) group waiting to leave its game area at the end of the phase.</summary>
/// <param name="Villain">The departing Kang (II), by object id.</param>
/// <param name="SourceGameArea">The game area the group leaves.</param>
/// <param name="Seats">The seats that move with it, in seat order; none when every player there was eliminated.</param>
/// <param name="CompletedScheme">
/// The completed stage 3B that caused the departure, or null when Kang (II) was defeated.
/// </param>
/// <param name="Destinations">
/// The game areas the group could join if it left now, in id order. The first player
/// chooses among them at the boundary, and a co-due departure resolved first can change them.
/// </param>
/// <param name="Reunion">
/// Whether no game area remains for it to join, so it goes to central stage 2B and the
/// table reunites at stage 4A (11008b).
/// </param>
public sealed record KangPendingDeparture(
    int Villain,
    int SourceGameArea,
    IReadOnlyList<int> Seats,
    int? CompletedScheme,
    IReadOnlyList<int> Destinations,
    bool Reunion);
