using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;
internal sealed record SchedulePowerCommand(
    AbilityEffect.Power Effect, string Verb, Card Target, ImmutableArray<Card> Targets,
    long Amount, int AbilityIndex, int PowerOrdinal, bool AutomaticThwartTarget)
    : AbilityStructuralTransition;
