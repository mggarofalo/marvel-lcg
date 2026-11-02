namespace Marvel.View;

/// <summary>The villain-phase step resolving now and the seat whose turn of it this is.</summary>
/// <param name="Step">The villain-phase step: 1 threat, 2 activation, or 4 reveal.</param>
/// <param name="Seat">The seat the step is resolving for.</param>
/// <param name="GameArea">That seat's game area id, or null when it is in none.</param>
public sealed record VillainPhaseFocusDescriptor(int Step, int Seat, int? GameArea);
