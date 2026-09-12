using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityRuntimeQueries;
using static Marvel.Cards.Run.AbilityProjectedStateQueries;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Traces numeric operations through an area projection.</summary>
internal static class AbilityAreaNumericProjection
{
    internal static List<AreaProjectionState>? TryTraceDamage(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() is "dealDamage" or "dealAttackDamage")
        {
            var (cards, printedAmount) = EffectOf<AbilityEffect>(effect, projection.context) switch
            {
                AbilityEffect.Damage damage => (damage.Cards, damage.Amount),
                AbilityEffect.AttackDamage damage => (damage.Cards, damage.Amount),
                _ => throw new InvalidOperationException("Expected a compiled damage instruction"),
            };
            long amount = AbilityAmounts.SaturatingSum(
                AbilityAmounts.SaturatingMultiply(
                    Amount(printedAmount, projection.context), baseMultiplier),
                [EventModifier(projection.context, "eventDamage"),
                 effect.OperationName() == "dealAttackDamage"
                     || projection.context.Power == BasicPowers.AttackVerb
                        ? EventModifier(projection.context, "attackDamage")
                        : 0]);
            foreach (var state in states)
            {
                foreach (var target in ProjectedEvery(
                             cards, state, projection.context))
                {
                    projection.DealProjected(state, target, amount, repetitions);
                }
            }
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceMoveDamage(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() is "moveDamage" or "moveAttackDamage")
        {
            var instruction = EffectOf<AbilityEffect.MoveDamage>(effect, projection.context);
            long requested = AbilityAmounts.SaturatingMultiply(
                Amount(instruction.Amount, projection.context), baseMultiplier);
            foreach (var state in states)
            {
                var from = ProjectedFind(instruction.From, state, projection.context);
                var to = ProjectedFind(instruction.To, state, projection.context);
                if (from is null || to is null
                    || !AbilityProgramQueries.CanTakeDamage(
                        projection.context.World, projection.context.Program, to, projection.context.Source))
                {
                    continue;
                }
                for (long repeat = 0;
                     repeat < repetitions && state.DamageOf(from) > 0;
                     repeat++)
                {
                    long moved = Math.Min(state.DamageOf(from), requested);
                    state.Damage[from.ObjectId] = state.DamageOf(from) - moved;
                    projection.DealProjected(state, to, moved, 1);
                }
            }
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceHeal(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "heal")
        {
            var instruction = EffectOf<AbilityEffect.Heal>(effect, projection.context);
            foreach (var state in states)
            {
                var healed = ProjectedFind(instruction.Card, state, projection.context);
                if (healed is null)
                {
                    continue;
                }
                long amount = AbilityAmounts.SaturatingMultiply(
                    AbilityAmounts.SaturatingMultiply(
                        Amount(instruction.Amount, projection.context), baseMultiplier),
                    repetitions);
                state.Damage[healed.ObjectId] = Math.Max(
                    0, state.DamageOf(healed) - amount);
            }
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceRemoveThreat(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "removeThreat")
        {
            var instruction = EffectOf<AbilityEffect.RemoveThreat>(effect, projection.context);
            long amount = AbilityAmounts.SaturatingSum(
                AbilityAmounts.SaturatingMultiply(
                    Amount(instruction.Amount, projection.context), baseMultiplier),
                [EventModifier(projection.context, "eventThreatRemoval")]);
            foreach (var state in states)
            {
                foreach (var scheme in ProjectedEvery(
                             instruction.Schemes, state, projection.context))
                {
                    long removed = AbilityAmounts.SaturatingMultiply(amount, repetitions);
                    state.Threat[scheme.ObjectId] = Math.Max(
                        0, state.ThreatOf(scheme) - removed);
                    projection.couldDiscard |= state.ThreatOf(scheme) == 0
                        && projection.context.World.Facts.Kind(scheme.FaceId)
                            == CardKind.EncounterSideScheme
                        && DefeatTreeChangesArea(scheme, projection.queried, projection.context);
                    if (state.ThreatOf(scheme) == 0
                        && projection.context.World.Facts.Kind(scheme.FaceId)
                            == CardKind.EncounterSideScheme)
                    {
                        projection.MarkDiscardedTree(state, scheme);
                    }
                }
            }
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceForEach(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "forEach")
        {
            if (CurrentlyZeroForEach(effect, projection.context))
            {
                return states;
            }
            var repeated = EffectBody(effect);
            long count = ForEachCount(effect, projection.context);
            return repeated.OperationName() is "dealDamage" or "dealAttackDamage"
                or "removeThreat"
                ? projection.Trace(
                    repeated, states, repetitions,
                    AbilityAmounts.SaturatingMultiply(baseMultiplier, count))
                : projection.Trace(
                    repeated, states,
                    AbilityAmounts.SaturatingMultiply(repetitions, count), baseMultiplier);
        }
        return null;
    }
}
