using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityContinuationCodec;
using static Marvel.Cards.Run.AbilityContinuationWireCodec;

namespace Marvel.Cards.Run;

internal static class LegacyChoiceContinuation
{
    internal static AbilityContinuationTransition Begin(
        AbilityProgram program, Card source, PhaseStep? step, AbilityType? tier,
        int from, bool eachPlayerFrame, bool finalPlayer,
        AbilityStructuralContext context)
    {
        var persisted = LegacyStepData.From(step, source, context.Player);
        var (ability, ordinal) = ChoiceAbility(program, source, persisted.Face, tier);
        var restored = RestoreState(
            context.Expressions.World.Cards, persisted.Discarded, persisted.Results,
            persisted.Actor, source.FaceId);
        var frames = ChoiceFrames(ability, from, eachPlayerFrame, finalPlayer);
        var state = new AbilityContinuationState(
            new(persisted.Face, tier, ordinal), frames, from,
            persisted.Seat, persisted.Player, persisted.Actor, persisted.FinalStep,
            finalPlayer, eachPlayerFrame, !frames.IsEmpty, persisted.Trigger,
            persisted.SurgeGained, persisted.Occurrence,
            restored.Discarded.Select(card => card.ObjectId).ToImmutableArray(),
            restored.Results,
            new(source.ObjectId, source.Area.Id, source.Incarnation), restored.Chosen,
            restored.CrisisIgnoringThwarts, [], AbilityResumeReason.Choice);
        return frames.IsEmpty
            ? new ResumeComplete(state with { HasContinuation = false })
            : Advance(context, ability, state, new AbilityStructuralObservation(false));
    }

    private static (CompiledCardAbility Ability, int Ordinal) ChoiceAbility(
        AbilityProgram program, Card source, string face, AbilityType? tier)
    {
        var written = AbilitiesOn(program, source, face)
            .Where(ability => tier is null || ability.Trigger.Timing == tier).ToList();
        var ability = written.FirstOrDefault(candidate =>
                AbilityChoiceAnalysis.Choices(candidate.Effect).Any())
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no choice waiting on an answer");
        return (ability, written.IndexOf(ability));
    }

    private static ImmutableArray<AbilityStructuralFrame> ChoiceFrames(
        CompiledCardAbility ability, int from, bool eachPlayerFrame, bool finalPlayer) =>
        ability.Effect is AbilityEffect.Sequence sequence
        && (!eachPlayerFrame || finalPlayer)
            ? [new SequenceFrame(from, sequence.Effects.Length)]
            : [];

    private sealed record LegacyStepData(
        string Face, IReadOnlyList<int>? Discarded,
        IReadOnlyDictionary<string, long>? Results, int Actor, int Seat, int Player,
        bool FinalStep, string Trigger, bool SurgeGained, Occurrence Occurrence)
    {
        internal static LegacyStepData From(
            PhaseStep? step, Card source, int defaultPlayer)
        {
            if (step is not { } current)
            {
                return new LegacyStepData(
                    source.FaceId, null, null, -1, defaultPlayer, defaultPlayer,
                    false, string.Empty, false,
                    new Occurrence(
                        0, [Steps.ChooseOption], Subject: source.ObjectId,
                        Player: defaultPlayer));
            }
            string face = string.IsNullOrEmpty(current.AbilityFace)
                ? source.FaceId
                : current.AbilityFace;
            string trigger = current.Trigger;
            return new LegacyStepData(
                face, current.Discarded, current.AbilityResults, current.AbilityActor,
                current.Seat, current.AbilityPlayer, current.FinalStep, trigger,
                current.SurgeGained,
                current.AbilityOccurrence ?? new Occurrence(
                    0, [trigger], Subject: source.ObjectId, Player: current.Seat));
        }
    }
}
