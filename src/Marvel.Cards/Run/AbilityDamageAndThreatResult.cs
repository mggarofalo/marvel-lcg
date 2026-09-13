using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed record AbilityDamageAndThreatResult(
    bool Handled, long? Healed, long? Remaining, bool ResolveEffect,
    AbilityDamageAndThreatSuspension Suspension, ImmutableArray<Card> Attacked)
{
    internal static AbilityDamageAndThreatResult NotHandled { get; } =
        new(false, null, null, false, AbilityDamageAndThreatSuspension.None, []);
}
