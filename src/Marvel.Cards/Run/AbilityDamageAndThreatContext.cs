using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed record AbilityDamageAndThreatContext(
    AbilityExpressionContext Expressions, AbilityProgram Program, string Trigger,
    List<GameEvent> Events, Card? AbilityActor, Card? PowerActor, string? Power,
    bool HasContinuation, ThreatPlacement? ImminentThreat, PendingAbility? ResolutionAbility,
    long Incoming, IThreatCardAbilities ThreatAbilities)
{
    internal World World => Expressions.World;
    internal Card Source => Expressions.Source;
    internal Occurrence Occurrence => Expressions.Occurrence;
    internal int Player => Expressions.Player;

    internal void ResolveEffect()
    {
        if (ResolutionAbility is { } ability) Occurrence.Resolve(ability);
    }
}
