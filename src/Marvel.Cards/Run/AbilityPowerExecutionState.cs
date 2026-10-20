using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Invocation facts retained until every node of a labelled power has finished.</summary>
internal sealed record AbilityPowerExecutionState(
    DecodedPowerContinuation Decoded, AbilityPowerContinuation Continuation,
    string Kind, int? Actor, long Amount, ImmutableArray<ContinuousEffect> Modifiers)
{
    internal ImmutableArray<int> Attacked { get; init; } = [];
    internal int? ThwartActorUsingAttack { get; init; }
}
