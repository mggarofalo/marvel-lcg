using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed record AbilityPowerContinuation(
    AbilityContinuationState Capture, int PowerOrdinal, ImmutableArray<int> Targets)
    : CardPowerContinuation
{
    internal ImmutableArray<AbilityContinuationCardBinding> SelectedTargets { get; init; }
    internal ImmutableArray<Attacked> DeclaredRecipients { get; init; }
}
