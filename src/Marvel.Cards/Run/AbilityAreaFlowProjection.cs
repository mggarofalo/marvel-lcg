using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityRuntimeQueries;
using static Marvel.Cards.Run.AbilityProjectedStateQueries;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Traces flow operations through an area projection.</summary>
internal static class AbilityAreaFlowProjection
{
    internal static List<AreaProjectionState>? TryTraceAnd(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "and")
        {
            if (repetitions > 1)
            {
                for (long repeat = 0; repeat < repetitions; repeat++)
                {
                    states = projection.Trace(
                        effect, states, 1, baseMultiplier);
                }
                return states;
            }
            var simultaneous = OrderedEffects(effect).ToList();
            const int MaximumProjectedAndEffects = 12;
            if (simultaneous.Count > MaximumProjectedAndEffects)
            {
                throw new RulesNotImplementedException(
                    $"'{projection.context.Source.FaceId}' has an and-group with "
                    + $"{simultaneous.Count} effects; projecting more than "
                    + $"{MaximumProjectedAndEffects} orders is not implemented");
            }

            var frontier = states
                .Select(state => (Mask: 0UL, State: state.Clone()))
                .ToList();
            for (int depth = 0; depth < simultaneous.Count; depth++)
            {
                var next = new List<(ulong Mask, AreaProjectionState State)>();
                foreach (var (mask, state) in frontier)
                {
                    for (int index = 0; index < simultaneous.Count; index++)
                    {
                        ulong bit = 1UL << index;
                        if ((mask & bit) != 0)
                        {
                            continue;
                        }
                        foreach (var projected in projection.Trace(
                                     simultaneous[index], [state.Clone()],
                                     baseMultiplier: baseMultiplier))
                        {
                            next.Add((mask | bit, projected));
                        }
                    }
                }
                frontier = next
                    .GroupBy(candidate =>
                        (candidate.Mask, candidate.State.Key()))
                    .Select(group => group.First()).ToList();
            }
            return AreaProjectionState.Distinct(
                frontier.Select(candidate => candidate.State));
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceConditional(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "if")
        {
            var test = ConditionalOf(effect, projection.context).Test;
            var branched = new List<AreaProjectionState>();
            foreach (var state in states)
            {
                bool? projected = ProjectedTest(test, state, projection.context);
                var branches = projected is { } result
                    ? ConditionalBranch(effect, result ? "then" : "else") is { } taken
                        ? [taken]
                        : []
                    : ReachableMutationBranches(effect, projection.context).ToList();
                if (branches.Count == 0)
                {
                    branched.Add(state);
                    continue;
                }
                foreach (var branch in branches)
                {
                    branched.AddRange(projection.Trace(
                        branch, [state.Clone()], repetitions, baseMultiplier));
                }
            }
            return AreaProjectionState.Distinct(branched);
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceDependent(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() is "then" or "otherwise")
        {
            var predecessor = EffectBody(effect);
            var dependent = EffectFollowing(effect);
            var required = effect.OperationName() == "then"
                ? AbilityProjectionResolution.Full : AbilityProjectionResolution.None;
            var branched = new List<AreaProjectionState>();
            foreach (var state in states)
            {
                var outcome = ProjectedResolution(
                    predecessor, state, projection.context);
                var projected = outcome == AbilityProjectionResolution.None
                    ? [state]
                    : projection.Trace(
                        predecessor, [state], repetitions,
                        baseMultiplier);
                if (outcome == required)
                {
                    projected = projection.Trace(
                        dependent, projected, repetitions,
                        baseMultiplier);
                }
                branched.AddRange(projected);
            }
            return AreaProjectionState.Distinct(branched);
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceChoice(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() is "choose" or "chooseCard")
        {
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTracePower(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() is "attack" or "thwart")
        {
            var instruction = EffectOf<AbilityEffect.Power>(effect, projection.context);
            var projected = new List<AreaProjectionState>();
            foreach (var state in states)
            {
                var target = ProjectedFind(
                    instruction.Target!, state, projection.context);
                if (target is null)
                {
                    projected.Add(state);
                    continue;
                }
                var prior = projection.context;
                try
                {
                    projection.context = projection.context.WithChosen(target);
                    projected.AddRange(projection.Trace(
                        EffectBody(effect), [state],
                        repetitions, baseMultiplier));
                }
                finally
                {
                    projection.context = prior;
                }
            }
            return projected;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceThwartGroup(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "thwartSchemes")
        {
            var instruction = EffectOf<AbilityEffect.ThwartGroup>(effect, projection.context);
            var projected = new List<AreaProjectionState>();
            foreach (var state in states)
            {
                var schemes = ProjectedEvery(
                    instruction.Schemes, state, projection.context);
                if (schemes.Count == 0)
                {
                    projected.Add(state);
                    continue;
                }
                var prior = projection.context;
                try
                {
                    projection.context = projection.context.WithChosen(schemes[0]).WithPowerTargets(schemes);
                    projected.AddRange(projection.Trace(
                        ((AbilityEffect.ThwartGroup)effect).Thwart, [state],
                        repetitions, baseMultiplier));
                }
                finally
                {
                    projection.context = prior;
                }
            }
            return projected;
        }
        return null;
    }
}
