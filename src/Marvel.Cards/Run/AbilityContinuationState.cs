using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;

internal sealed record AbilityContinuationState(
    AbilityContinuationAddress Address,
    ImmutableArray<AbilityStructuralFrame> Frames,
    int Position, int Player, int AbilityPlayer, int AbilityActor,
    bool FinalStep, bool FinalPlayer, bool EachPlayerFrame, bool HasContinuation,
    string Trigger, bool SurgeGained, Occurrence? Occurrence,
    ImmutableArray<int> Discarded, ImmutableDictionary<string, long> Results,
    AbilityContinuationCardBinding Source, AbilityContinuationCardBinding? Chosen,
    ImmutableHashSet<int> CrisisIgnoringThwarts, ImmutableArray<int> ActivationIds,
    AbilityResumeReason Reason);
