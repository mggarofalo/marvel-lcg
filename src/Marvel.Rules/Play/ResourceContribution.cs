using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One accepted resource contribution, bound to its original card copy and mode.</summary>
internal sealed record ResourceContribution(Card Card, int Incarnation, bool IsGenerator);
