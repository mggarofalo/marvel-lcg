using Marvel.Cards.Dsl;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed record ActivationEffect(
    int Source, int Player, AbilityType? Tier, AbilityEffect Effect, int Altered,
    int AbilityActor);
