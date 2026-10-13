namespace Marvel.Rules.Events;

/// <summary>A completed engine instruction or simultaneous phase step; a wire type.</summary>
/// <remarks>Batch identity and receipt spelling are engine choices, scoped to the owning event stream.</remarks>
public sealed record ActivityCompleted(
    int Batch, IReadOnlyList<ActivityParticipant> Participants,
    ActivityPhaseStep? PhaseStep = null) : GameEvent;
