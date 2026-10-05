using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityCostSelection;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionRestoration
{
    internal static int AbilityOrdinal(this AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast)
    {
        if (cast.AbilityOrdinal >= 0)
        {
            return cast.AbilityOrdinal;
        }

        return AbilityContinuationWireCodec.OrdinalForNode(
            execution.program, cast.Source, cast.AbilityFace, cast.Tier,
            cast.StructuralPath, node);
    }

    internal static ImmutableArray<CompiledCardAbility> AbilitiesOn(this AbilityResolutionExecution execution, Card source, string? face) =>
        AbilityContinuationWireCodec.AbilitiesOn(execution.program, source, face);

    internal static void TrackResolution(this AbilityResolutionExecution execution, AbilityResolutionState cast, CompiledCardAbility ability)
    {
        var sameTier = execution.AbilitiesOn(cast.Source, cast.AbilityFace)
            .Where(candidate => candidate.Trigger.Timing == ability.Trigger.Timing)
            .ToList();
        int ordinal = sameTier.FindIndex(candidate => ReferenceEquals(candidate, ability));
        if (ordinal < 0)
        {
            ordinal = sameTier.IndexOf(ability);
        }
        if (ordinal < 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot identify the ability whose resolution is tracked");
        }
        cast.RestoreAbility(ordinal, []);
        cast.TrackResolution(ordinal);
    }

    internal static CompiledCardAbility AbilityAt(this AbilityResolutionExecution execution,
        Card source, AbilityType? tier, int ordinal, string? face = null) =>
        AbilityContinuationWireCodec.AbilityAt(execution.program, source, tier, ordinal, face);

    internal static void RestorePersisted(this AbilityResolutionExecution execution, AbilityResolutionState cast, PhaseStep? continuation)
    {
        if (continuation is not { } step)
        {
            return;
        }
        execution.ApplyRestored(cast, AbilityContinuationRestoration.RestoreState(
            cast.World.Cards, step.Discarded, step.AbilityResults,
            step.AbilityActor, cast.Source.FaceId));
    }

    internal static void RestorePersisted(this AbilityResolutionExecution execution,
        AbilityResolutionState cast, IReadOnlyList<int>? discarded,
        IReadOnlyDictionary<string, long>? results)
    {
        execution.ApplyRestored(cast, AbilityContinuationRestoration.RestoreState(
            cast.World.Cards, discarded, results, -1, cast.Source.FaceId));
    }

    internal static void ApplyRestored(this AbilityResolutionExecution execution, AbilityResolutionState cast, RestoredContinuationState state)
    {
        cast.Discarded.Clear();
        cast.Discarded.AddRange(state.Discarded);
        foreach (var (name, value) in state.Results)
        {
            cast.Results[name] = value;
        }
        cast.RestoreCrisisIgnoringThwarts(state.CrisisIgnoringThwarts);
        cast.RestoreSourceIncarnation(state.SourceIncarnation);
        cast.RestoreSourceState(state.SourceState);
        if (state.Chosen is { } chosen)
            cast.RestorePersistedSelection(
                cast.World.Cards[chosen.ObjectId], chosen.AreaId, chosen.Incarnation,
                overwriteChosen: false);
        cast.AbilityActor = state.Actor;
    }

    internal static PhaseStep? ContinuationStep(this AbilityResolutionExecution execution,
        World world, Card source, int stoppedAt, AbilityType? tier)
        => AbilityContinuationWireCodec.ContinuationStep(
            world.Agenda.Current, world.Agenda.Outstanding, source.ObjectId, stoppedAt, tier);
}
