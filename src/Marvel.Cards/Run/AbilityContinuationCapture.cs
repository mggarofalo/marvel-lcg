using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;

// Concrete capture facts cross the executor boundary. This deliberately contains
// values, not an AbilityResolutionState, callback, or service object.
internal sealed record AbilityContinuationCapture(
    int Source, int SourceIncarnation, AbilityContinuationAddress Address,
    ImmutableArray<AbilityStructuralFrame> Frames, int Position, int Player, int AbilityPlayer,
    int AbilityActor, bool FinalStep, bool FinalPlayer, bool EachPlayerFrame,
    bool HasContinuation, string Trigger, bool SurgeGained, Occurrence Occurrence,
    ImmutableArray<int> Discarded, ImmutableDictionary<string, long> Results,
    AbilityContinuationCardBinding? Chosen);
