using System.Collections.Immutable;

namespace Marvel.Cards.Run;

/// <summary>The validated ready cards chosen for an any-number exhaustion.</summary>
internal sealed record ExhaustSelectedCommand(ImmutableArray<int> Targets) : AbilityStructuralTransition;
