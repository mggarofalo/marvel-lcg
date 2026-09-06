using static Marvel.Cards.Run.AbilityEffectStructure;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    /// <summary>
    /// Runs what is left of the ability after the answered choice.
    /// </summary>
    /// <remarks>
    /// The chosen option has already run; this is the rest of the sequence it
    /// was a step of. If the rest holds another choice, it suspends again and
    /// the step it schedules says where to pick up next.
    /// </remarks>
    private List<GameEvent> Continue(Card source, AbilityResolutionState cast, int from)
    {
        if (cast.Suspended)
        {
            cast.CompleteResolution();
            DiscardEvent(source, cast);
            return cast.Events;
        }

        if (cast.AbilityOrdinal >= 0 && cast.StructuralPath.Count > 0)
        {
            var ability = AbilityAt(
                source, cast.Tier, cast.AbilityOrdinal, cast.AbilityFace);
            var persisted = AbilityContinuationCodec.Step(
                Capture(cast, cast.AbilityOrdinal), Steps.ResumeAbility,
                cast.World.Agenda.Current?.Round ?? 0);
            var state = AbilityContinuationCodec.Decode(
                program, source, persisted, cast.Tier).State;
            return ResumeContinuation(
                cast, source,
                new ContinueAfterResumedNode(ability, state, EffectApplied: false));
        }

        var legacy = ContinuationStep(cast.World, source, from, cast.Tier);
        return ResumeContinuation(cast, source,
            AbilityContinuationCodec.BeginLegacyChoiceResume(
                program, source, legacy, cast.Tier, from,
                cast.EachPlayerFrame, cast.FinalPlayer, StructuralContext(cast)));
    }

    /// <summary>A fresh resolution of one card's ability, by one player.</summary>
    private static AbilityResolutionState Resolving(
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
    private static AbilityResolutionState Resuming(
        World world, Card source, int player, AbilityType? tier, bool finalStep = false,
        Occurrence? continuation = null)
    {
        var cast = Resolving(world, source, player, tier, finalStep, continuation);
        if (world.Agenda.Current?.Discarded is { } discarded)
        {
            cast.Discarded.AddRange(discarded.Select(id => world.Cards[id]));
        }
        return cast;
    }

    private static IEnumerable<AbilityEffect> Choices(AbilityEffect node) =>
        AbilityInitiation.Choices(node);

    private IEnumerable<AbilityEffect> ActiveChoices(AbilityEffect node, AbilityResolutionState cast) =>
        AbilityInitiation.ActiveChoices(node, AdmissionContext(cast));

    private static bool IsChoice(AbilityEffect node) => AbilityInitiation.IsChoice(node);

    private bool SuspendsInsideAnd(
        AbilityEffect node, AbilityResolutionState cast, bool stateMayChange = false,
        bool bindingMayChange = false) =>
        AbilityInitiation.SuspendsInsideAnd(
            node, AdmissionContext(cast), stateMayChange, bindingMayChange);

}
