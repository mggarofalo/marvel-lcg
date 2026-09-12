using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

// One immutable read of a live resolution. The executor refreshes it after every
// command, so structural decisions never retain a stale view of the board.
internal sealed record AbilityStructuralContext(
    AbilityProgram Program,
    IResourceCardAbilities ResourceAbilities,
    IThreatCardAbilities ThreatAbilities,
    AbilityExpressionContext Expressions,
    AbilityReachabilityContext Reachability,
    string Trigger,
    string SourceFace,
    string AbilityFace,
    int Player,
    int Position,
    bool HasContinuation,
    AbilityType? Tier,
    string? Power,
    Card? AbilityActor,
    bool HasPendingDependency,
    ImmutableHashSet<AbilityEffect> CrisisIgnoringThwarts,
    ImmutableHashSet<int> PersistedCrisisIgnoringThwarts,
    ImmutableArray<AbilityStructuralFrame> Frames)
{
    internal AbilityAdmissionContext Admission() => new(
        Program, ResourceAbilities, Expressions, Reachability, Power, HasContinuation);
}

internal enum AbilityStructuralOutcome { None, Partial, Full }

// Live frames are typed. Their legacy string encoding is confined to the
// continuation codec at the persistence boundary.
internal abstract record AbilityStructuralFrame;
internal sealed record SequenceFrame(int Next, int Count) : AbilityStructuralFrame;
internal sealed record SimultaneousFrame(int Current, ImmutableArray<int> Remaining, ImmutableArray<int> Completed)
    : AbilityStructuralFrame;
internal sealed record DependentFrame(bool OnFull, bool Predecessor, AbilityStructuralOutcome? Outcome)
    : AbilityStructuralFrame;
internal sealed record ConditionalFrame(bool Then) : AbilityStructuralFrame;
internal sealed record ForEachFrame(long Next, long Count) : AbilityStructuralFrame;
internal sealed record EachTimeFrame(long Next, long Count, int? DiscardedCard) : AbilityStructuralFrame;
internal sealed record ChoiceFrame(int? Option, int? Card) : AbilityStructuralFrame;
internal sealed record ChoiceOtherwiseFrame : AbilityStructuralFrame;
internal sealed record DefenseFrame : AbilityStructuralFrame;
internal sealed record EachPlayerFrame(int Player, bool Final) : AbilityStructuralFrame;

internal sealed record AbilityStructuralObservation(bool Suspended, Card? Discarded = null);

internal abstract record AbilityStructuralTransition;
internal sealed record RunLeaf(
    AbilityEffect Effect, ImmutableArray<AbilityStructuralFrame> Frames,
    int Position, bool HasContinuation,
    AbilityAdmissionResult? Admission = null) : AbilityStructuralTransition;
internal sealed record RunCombinedForEach(AbilityEffect Effect, long Multiplier)
    : AbilityStructuralTransition;
internal sealed record RunChoice(
    AbilityEffect Effect, ChoiceFrame Frame, Card? Selection,
    bool BindsPlayerSelection, AbilityStructuralOutcome? PendingOutcome,
    AbilityAdmissionResult Admission) : AbilityStructuralTransition;
internal sealed record Ask(AbilityEffect Choice, ImmutableArray<AbilityStructuralFrame> Frames)
    : AbilityStructuralTransition;
internal sealed record ScheduleEachPlayer(
    AbilityEffect.EachPlayer Effect, EachPlayerFrame Frame) : AbilityStructuralTransition;
internal sealed record DelayAfterActivation(AbilityEffect.AfterActivation Effect)
    : AbilityStructuralTransition;
internal sealed record RunOrdered(
    ImmutableArray<AbilityEffect> Effects, ImmutableArray<SimultaneousFrame> Frames)
    : AbilityStructuralTransition;
internal sealed record ResolveSpecialsCommand(ImmutableArray<int> Targets) : AbilityStructuralTransition;
internal sealed record ChooseTopForHandCommand(int Selected, ImmutableArray<int> Top)
    : AbilityStructuralTransition;
internal sealed record ShuffleDiscardCommand(ImmutableArray<int> Targets)
    : AbilityStructuralTransition;
internal sealed record PayOrCommand(bool Pay) : AbilityStructuralTransition;
internal sealed record AssignedDamageCommand(ImmutableDictionary<int, long> Assigned)
    : AbilityStructuralTransition;
internal sealed record ThwartSelectionCommand(
    int Scheme, ImmutableArray<int> Resolving, ImmutableArray<int> Discard, long PowerAmount)
    : AbilityStructuralTransition;
internal sealed record MakeTheCallCommand(int Ally) : AbilityStructuralTransition;
internal sealed record StartSequenceCommand(AbilityEffect.Sequence Effect) : AbilityStructuralTransition;
internal sealed record StartDependentCommand(AbilityEffect.Dependent Effect) : AbilityStructuralTransition;
internal sealed record StartForEachCommand(AbilityEffect.ForEach Effect) : AbilityStructuralTransition;
internal sealed record StartEachTimeCommand(AbilityEffect.EachTime Effect) : AbilityStructuralTransition;
internal sealed record RunDefenseCommand(AbilityEffect.Power Effect) : AbilityStructuralTransition;
internal sealed record SchedulePowerCommand(
    AbilityEffect.Power Effect, string Verb, Card Target, ImmutableArray<Card> Targets,
    long Amount, int AbilityIndex, int PowerOrdinal, bool AutomaticThwartTarget)
    : AbilityStructuralTransition;
internal sealed record ActivationTarget(Card Enemy, int Seat);
internal sealed record ScheduleActivationsCommand(
    AbilityEffect.ActivateEnemies Effect, ImmutableArray<ActivationTarget> Targets,
    int Against, bool First, bool Dynamic) : AbilityStructuralTransition;
internal sealed record DiscardEachTime(AbilityEffect.DiscardTop Effect, EachTimeFrame Frame)
    : AbilityStructuralTransition;
internal sealed record Complete(
    ImmutableArray<AbilityStructuralFrame> Frames,
    AbilityAdmissionResult? Admission = null) : AbilityStructuralTransition;
internal sealed record Rejected(string Reason) : AbilityStructuralTransition;
internal sealed record Unsupported(string Reason) : AbilityStructuralTransition;
