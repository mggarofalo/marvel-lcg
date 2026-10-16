using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed record AbilityPowerContinuation(
    AbilityContinuationState Capture, int PowerOrdinal, ImmutableArray<int> Targets)
    : CardPowerContinuation;
