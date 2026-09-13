using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One engaged minion and its ordered, retained hosted descendants.</summary>
/// <param name="Minion">The root that engages the next player.</param>
/// <param name="Hosted">Descendants in parent-before-child movement order.</param>
public sealed record EliminationRelocation(int Minion, ImmutableArray<int> Hosted);
