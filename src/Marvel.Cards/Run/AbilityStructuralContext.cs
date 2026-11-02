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
    internal bool IsMandatoryEncounter =>
        !AbilityCardQueries.IsPlayerCard(Expressions.World.Facts, Expressions.Source)
        && Tier is { } tier && AbilityTypes.IsMandatory(tier);

    // rr:first-player.2: "If an encounter card requires a card ability to be
    // resolved, a game function to be performed, or a choice to be made but does
    // not specify which player should act, the first player does so." A
    // resolution bound to no player is that unspecified case.
    internal bool FirstPlayerChooses(AbilityEffect choice) =>
        choice is AbilityEffect.ChooseCard { FirstPlayerChooses: true } || Player < 0;

    /// <summary>Whether a card choice offers its legal cards without filtering by later text.</summary>
    internal bool ChoosesWithoutContinuation(AbilityEffect.ChooseCard choice) =>
        IsMandatoryEncounter || AbilityAdmission.IsDiscardReceiptChoice(choice.From);

    internal AbilityAdmissionContext Admission() => new(
        Program, ResourceAbilities, Expressions, Reachability, Power, HasContinuation);
}
