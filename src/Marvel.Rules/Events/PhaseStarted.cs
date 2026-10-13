namespace Marvel.Rules.Events;

/// <summary>The game entered a phase in an established round; a wire type.</summary>
/// <remarks>The phase spelling and marker are engine representation choices.</remarks>
public sealed record PhaseStarted(string Phase) : GameEvent;
