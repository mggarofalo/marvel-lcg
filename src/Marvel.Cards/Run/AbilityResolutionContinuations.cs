using static Marvel.Cards.Run.AbilityEffectStructure;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionContinuations
{
    /// <summary>
    /// Runs what is left of the ability after the answered choice.
    /// </summary>
    /// <remarks>
    /// The chosen option has already run; this is the rest of the sequence it
    /// was a step of. If the rest holds another choice, it suspends again and
    /// the step it schedules says where to pick up next.
    /// </remarks>
    internal static List<GameEvent> Continue(this AbilityResolutionExecution execution, Card source, AbilityResolutionState cast, int from)
    {
        if (cast.Suspended)
        {
            cast.CompleteResolution();
            execution.DiscardEvent(source, cast);
            return cast.Events;
        }

        if (cast.AbilityOrdinal >= 0 && cast.StructuralPath.Count > 0)
        {
            var ability = execution.AbilityAt(
                source, cast.Tier, cast.AbilityOrdinal, cast.AbilityFace);
            var persisted = AbilityContinuationCodec.Step(
                execution.Capture(cast, cast.AbilityOrdinal), Steps.ResumeAbility,
                cast.World.Agenda.Current?.Round ?? 0);
            var state = AbilityContinuationWireCodec.Decode(
                execution.program, source, persisted, cast.Tier).State;
            return execution.ResumeContinuation(
                cast, source,
                new ContinueAfterResumedNode(ability, state, EffectApplied: false));
        }

        var legacy = execution.ContinuationStep(cast.World, source, from, cast.Tier);
        return execution.ResumeContinuation(cast, source,
            AbilityContinuationCodec.BeginLegacyChoiceResume(
                execution.program, source, legacy, cast.Tier, from,
                cast.EachPlayerFrame, cast.FinalPlayer, execution.StructuralContext(cast)));
    }

    /// <summary>A fresh resolution of one card's ability, by one player.</summary>
    internal static AbilityResolutionState Resolving(this AbilityResolutionExecution execution,
        World world, Card source, int player, AbilityType? tier, bool finalStep = false,
        Occurrence? continuation = null) =>
        new(world,
            source,
            continuation ?? new Occurrence(
                0, [Steps.CardRevealed], Subject: source.ObjectId, Player: player),
            player,
            [])
        {
            Tier = tier,
            FinalStep = finalStep,
        };

    /// <summary>A suspended resolution with its persisted card bindings restored.</summary>
    internal static AbilityResolutionState Resuming(this AbilityResolutionExecution execution,
        World world, Card source, int player, AbilityType? tier, bool finalStep = false,
        Occurrence? continuation = null)
    {
        var cast = execution.Resolving(world, source, player, tier, finalStep, continuation);
        if (world.Agenda.Current?.Discarded is { } discarded)
        {
            cast.Discarded.AddRange(discarded.Select(id => world.Cards[id]));
        }
        return cast;
    }

    internal static IEnumerable<AbilityEffect> Choices(this AbilityResolutionExecution execution, AbilityEffect node) =>
        AbilityChoiceAnalysis.Choices(node);

    internal static IEnumerable<AbilityEffect> ActiveChoices(this AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast) =>
        AbilityChoiceAnalysis.ActiveChoices(node, execution.AdmissionContext(cast));

    internal static bool IsChoice(this AbilityResolutionExecution execution, AbilityEffect node) => AbilityChoiceAnalysis.IsChoice(node);

    internal static bool SuspendsInsideAnd(this AbilityResolutionExecution execution,
        AbilityEffect node, AbilityResolutionState cast, bool stateMayChange = false,
        bool bindingMayChange = false) =>
        AbilityChoiceAnalysis.SuspendsInsideAnd(
            node, execution.AdmissionContext(cast), stateMayChange, bindingMayChange);

}
