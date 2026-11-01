namespace Marvel.View;

/// <summary>A Kang (II) group that leaves its game area at the end of the phase.</summary>
/// <param name="Villain">The departing Kang (II), by object id.</param>
/// <param name="SourceGameArea">The game area the group leaves.</param>
/// <param name="Seats">The seats that move with it; empty when every player there was eliminated.</param>
/// <param name="CompletedScheme">The completed stage 3B that caused it, or null when Kang (II) was defeated.</param>
/// <param name="Destinations">The game areas it could join if it left now; the first player chooses at the boundary.</param>
/// <param name="Reunion">Whether no game area remains for it to join, so the table reunites at stage 4A.</param>
/// <param name="Summary">The engine-authored sentence naming the cause, the players and the area they leave.</param>
public sealed record KangDepartureDescriptor(
    int Villain,
    int SourceGameArea,
    IReadOnlyList<int> Seats,
    int? CompletedScheme,
    IReadOnlyList<int> Destinations,
    bool Reunion,
    string Summary);
