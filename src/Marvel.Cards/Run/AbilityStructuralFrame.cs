using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

// Each frame retains the authored parent position across an engine decision.
internal abstract record AbilityStructuralFrame
{
    internal AbilityStructuralOutcome? CompletedOutcome { get; init; }
}
