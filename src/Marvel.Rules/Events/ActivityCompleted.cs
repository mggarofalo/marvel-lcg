namespace Marvel.Rules.Events;

/// <summary>A completed engine instruction or simultaneous phase step; a wire type.</summary>
/// <remarks>Batch identity and receipt spelling are engine choices, scoped to one response.</remarks>
public sealed record ActivityCompleted(
    int Batch, IReadOnlyList<ActivityParticipant> Participants,
    ActivityPhaseStep? PhaseStep = null) : GameEvent;
