using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;
internal sealed record SimultaneousContinuationFrame(
    AbilityEffect.Simultaneous Parent, ImmutableArray<int> Remaining)
    : AbilityContinuationFrame;
