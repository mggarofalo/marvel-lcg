using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;

// Continuation wire data is intentionally decoded against the compiled program.
// Paths are engine-chosen save data, not an alternate executable syntax.
internal sealed record AbilityContinuationAddress(string Face, AbilityType? Tier, int Ordinal);
internal sealed record AbilityContinuationCardBinding(int ObjectId, int AreaId, int Incarnation);
internal enum AbilityResumeReason { Choice, CostProcedure, EffectProcedure, Activations, Power, EachPlayer }

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

internal sealed record AbilityContinuationWire(
    int Ordinal, ImmutableArray<string> Path, ImmutableArray<int> ActivationIds,
    ImmutableDictionary<string, long> Results, string Face, int Player, int Actor,
    Occurrence? Occurrence, bool FinalStep, bool FinalPlayer, bool EachPlayerFrame,
    bool HasContinuation, string Trigger, bool SurgeGained, ImmutableArray<int> Discarded);

internal sealed record DecodedAbilityContinuation(
    AbilityContinuationState State, CompiledCardAbility Ability, AbilityEffect Node,
    AbilityContinuationFacts Facts);
internal sealed record DecodedPowerContinuation(
    CompiledCardAbility Ability, AbilityEffect Body,
    ImmutableArray<AbilityStructuralFrame> Frames, int Ordinal);
internal sealed record DecodedEachPlayerContinuation(
    CompiledCardAbility Ability, AbilityEffect Body, ImmutableArray<AbilityStructuralFrame> Frames,
    int Ordinal, bool HasContinuation);
internal sealed record RestoredContinuationState(
    ImmutableArray<Card> Discarded, ImmutableDictionary<string, long> Results,
    int SourceIncarnation, AbilityContinuationCardBinding? Chosen, Card? Actor,
    ImmutableHashSet<int> CrisisIgnoringThwarts);

// Concrete capture facts cross the executor boundary. This deliberately contains
// values, not an AbilityResolutionState, callback, or service object.
internal sealed record AbilityContinuationCapture(
    int Source, int SourceIncarnation, AbilityContinuationAddress Address,
    ImmutableArray<AbilityStructuralFrame> Frames, int Position, int Player, int AbilityPlayer,
    int AbilityActor, bool FinalStep, bool FinalPlayer, bool EachPlayerFrame,
    bool HasContinuation, string Trigger, bool SurgeGained, Occurrence Occurrence,
    ImmutableArray<int> Discarded, ImmutableDictionary<string, long> Results,
    AbilityContinuationCardBinding? Chosen);
internal sealed record ActivationWaitResult(PhaseStep Step, bool Complete);
internal abstract record AbilityContinuationTransition;
internal sealed record RunResumedNode(
    CompiledCardAbility Ability, AbilityEffect Effect, AbilityContinuationState State,
    bool EffectApplied = false)
    : AbilityContinuationTransition;
internal sealed record ContinueAfterResumedNode(
    CompiledCardAbility Ability, AbilityContinuationState State, bool EffectApplied)
    : AbilityContinuationTransition;
internal sealed record DiscardForResumedEachTime(
    CompiledCardAbility Ability, AbilityEffect.EachTime Effect,
    EachTimeFrame Frame, AbilityContinuationState State)
    : AbilityContinuationTransition;
internal sealed record RestartAfterPaidCost(CompiledCardAbility Ability, AbilityContinuationState State)
    : AbilityContinuationTransition;
internal sealed record ResumeComplete(AbilityContinuationState State) : AbilityContinuationTransition;
internal sealed record ResumeRejected(string Reason) : AbilityContinuationTransition;
