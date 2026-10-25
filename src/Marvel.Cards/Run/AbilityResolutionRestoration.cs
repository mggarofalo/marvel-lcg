using static Marvel.Cards.Run.AbilityContinuationFrames;
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

        return AbilityContinuationFrames.OrdinalForNode(
            execution.program, cast.Source, cast.AbilityFace, cast.Tier,
            cast.StructuralPath, node);
    }

    internal static ImmutableArray<CompiledCardAbility> AbilitiesOn(this AbilityResolutionExecution execution, Card source, string? face) =>
        AbilityContinuationFrames.AbilitiesOn(execution.program, source, face);

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
        AbilityContinuationFrames.AbilityAt(execution.program, source, tier, ordinal, face);

    internal static void RestorePersisted(this AbilityResolutionExecution execution, AbilityResolutionState cast, PhaseStep? continuation)
    {
        if (continuation is not { } step)
        {
            return;
        }
        execution.RestorePersisted(cast, StateOf(step)!);
    }

    internal static void RestorePersisted(this AbilityResolutionExecution execution,
        AbilityResolutionState cast, AbilityContinuationState state)
    {
        cast.DamageDefeats = state.Memory.DamageDefeats;
        cast.Execution = state.Execution;
        cast.ThreatMovePairs = state.ThreatMovePairs;
        cast.PowerExecution = state.PowerExecution;
        if (state.PowerExecution is { } power)
        {
            cast.Attacked.Clear();
            cast.Attacked.AddRange(power.Attacked.Select(id => cast.World.Cards[id]));
        }
        cast.DamageReplacement = state.DamageReplacement;
        cast.TriggeringDefeat = state.Invocation.TriggeringDefeat;
        cast.TriggeringAttack = state.Invocation.TriggeringAttack;
        cast.TriggeringThreat = state.Invocation.TriggeringThreat;
        execution.ApplyRestored(cast,
            AbilityContinuationRestoration.RestoreState(cast.World.Cards, state));
    }

    internal static void ApplyRestored(this AbilityResolutionExecution execution, AbilityResolutionState cast, RestoredContinuationState state)
    {
        cast.ExhaustedForCost.UnionWith(state.ExhaustedForCost);
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
        => AbilityContinuationFrames.ContinuationStep(
            world.Agenda.Current, world.Agenda.Outstanding, source.ObjectId, stoppedAt, tier);
}
