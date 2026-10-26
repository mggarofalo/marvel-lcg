using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>One exact imminent defeat within a shared damage-step-six window.</summary>
public sealed record DefeatWindowSource(Occurrence Origin, int Incarnation);
