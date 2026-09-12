using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Public information about one seat.</summary>
public sealed record PlayerDescriptor(int Seat, string Name, bool Eliminated);
