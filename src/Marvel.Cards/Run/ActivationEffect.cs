using Marvel.Cards.Dsl;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed record ActivationEffect(
    AbilityContinuationState Capture, int Altered);
