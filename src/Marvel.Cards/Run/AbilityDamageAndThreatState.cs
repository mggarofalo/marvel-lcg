using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed class AbilityDamageAndThreatState
{
    internal AbilityStructuralOutcome? Outcome { get; set; }
    internal AbilityDamageCompletion? Damage { get; set; }
    internal ThreatBatch? Threat { get; set; }
    internal AbilityEffectBoundary? Boundary { get; set; }
    internal long? Healed { get; set; }
    internal long? SchemesCleared { get; set; }
    internal long? Remaining { get; set; }
    internal bool ResolveEffect { get; set; }
    internal AbilityDamageAndThreatSuspension Suspension { get; set; }
    internal List<Card> Attacked { get; } = [];
    internal AbilityDamageAndThreatResult ToResult() => new(true, Healed, Remaining, ResolveEffect, Suspension, [.. Attacked])
        { Threat = Threat, Outcome = Outcome, Boundary = Boundary, Damage = Damage, SchemesCleared = SchemesCleared };
}
