using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Printed card restrictions on a one-use cost reduction.</summary>
public sealed record CardCostFilter(CardKind? Kind = null, string? Trait = null);
