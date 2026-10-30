using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The exact defeated Kang and whole group awaiting one phase boundary.</summary>
public sealed record KangDeparture(
    OccurrenceCard Defeated, GameArea Source, IReadOnlyList<PlayArea> Group,
    int Round, PhaseTransitionKind Boundary, bool NextPhase = false,
    OccurrenceCard? CompletedScheme = null);
