using System.Collections.Immutable;

namespace Marvel.Rules.State;

/// <summary>Authored characteristics of a temporary blank card identity.</summary>
public sealed record EffectiveCardProfile(
    string Id, string Title, CardKind Kind, ImmutableArray<string> Traits,
    ImmutableDictionary<string, long> BaseValues);
