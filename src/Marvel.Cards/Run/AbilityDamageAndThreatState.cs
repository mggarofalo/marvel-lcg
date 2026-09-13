using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed class AbilityDamageAndThreatState
{
    internal long? Healed { get; set; }
    internal long? Remaining { get; set; }
    internal bool ResolveEffect { get; set; }
    internal AbilityDamageAndThreatSuspension Suspension { get; set; }
    internal List<Card> Attacked { get; } = [];
    internal AbilityDamageAndThreatResult ToResult() => new(true, Healed, Remaining, ResolveEffect, Suspension, [.. Attacked]);
}
