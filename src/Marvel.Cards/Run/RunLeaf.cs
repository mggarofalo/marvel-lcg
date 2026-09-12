using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;
internal sealed record RunLeaf(
    AbilityEffect Effect, ImmutableArray<AbilityStructuralFrame> Frames,
    int Position, bool HasContinuation,
    AbilityAdmissionResult? Admission = null) : AbilityStructuralTransition;
