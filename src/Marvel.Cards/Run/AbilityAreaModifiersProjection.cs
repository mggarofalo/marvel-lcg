using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityRuntimeQueries;
using static Marvel.Cards.Run.AbilityProjectedStateQueries;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Traces modifiers operations through an area projection.</summary>
internal static class AbilityAreaModifiersProjection
{
    internal static List<AreaProjectionState>? TryTraceGiveStatus(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "giveStatus")
        {
            var instruction = EffectOf<AbilityEffect.GiveStatus>(effect, projection.context);
            string status = instruction.Status;
            foreach (var state in states)
            {
                foreach (var target in ProjectedEvery(
                             instruction.Cards, state, projection.context))
                {
                    long limit = Statuses.Limit(
                        projection.context.World, projection.context.World.Facts, target, status);
                    long held = state.StatusOf(projection.context, target, status);
                    if (held >= limit)
                    {
                        continue;
                    }
                    state.Status[(target.ObjectId, status)] = held + 1;
                    if (status == Statuses.Tough)
                    {
                        state.Tough[target.ObjectId] = held + 1;
                    }
                    bool vulnerable = status is Statuses.Stunned
                            or Statuses.Confused
                        && StateFields.Modified(
                            projection.context.World, target, "vulnerable",
                            projection.context.World.Facts, projection.context.World.Players) > 0
                        && held + 1 >= limit && limit > 0;
                    if (vulnerable && projection.DiscardTreeChangesArea(target))
                    {
                        projection.couldDiscard = true;
                    }
                }
            }
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceGrantTrait(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "grantUntil"
            && EffectOf<AbilityEffect>(effect, projection.context) is AbilityEffect.GrantTrait traitGrant)
        {
            string trait = traitGrant.Trait;
            foreach (var state in states)
            {
                var target = ProjectedFind(
                    traitGrant.Cards, state, projection.context);
                if (target is null)
                {
                    continue;
                }
                if (!state.Traits.TryGetValue(
                        target.ObjectId, out var granted))
                {
                    state.Traits[target.ObjectId] = granted = [];
                }
                granted.Add(trait);
            }
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceGrantField(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "grantUntil"
            && EffectOf<AbilityEffect>(effect, projection.context) is AbilityEffect.GrantField fieldGrant)
        {
            string field = fieldGrant.Field;
            long amount = Amount(fieldGrant.Amount, projection.context);
            foreach (var state in states)
            {
                var target = ProjectedFind(
                    fieldGrant.Cards, state, projection.context);
                if (target is not null)
                {
                    var key = (target.ObjectId, field);
                    state.Modifiers[key] = AbilityAmounts.SaturatingSum(
                        state.Modifiers.GetValueOrDefault(key), [amount]);
                }
            }
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceEachPlayer(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "eachPlayer")
        {
            var prior = projection.context;
            try
            {
                foreach (int player in projection.context.World.PlayerOrder)
                {
                    projection.context = projection.context.WithPlayer(player);
                    states = projection.Trace(EffectBody(effect), states,
                        repetitions, baseMultiplier);
                }
                return states;
            }
            finally { projection.context = prior; }
        }
        return null;
    }
}
