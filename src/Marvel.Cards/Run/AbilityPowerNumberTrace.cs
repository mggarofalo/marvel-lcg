using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using PowerReachability = Marvel.Rules.Play.RuleProjection<Marvel.Cards.Run.AbilityPowerState>;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityPowerOutcomeTrace;
using static Marvel.Cards.Run.AbilityPowerStateMutation;
using static Marvel.Cards.Run.AbilityPowerHealthTrace;
namespace Marvel.Cards.Run;

internal static class AbilityPowerNumberTrace
{
    internal static bool TryPowerTest(
        AbilityCondition test, Dictionary<int, long> schemeThreat,
        AbilityAdmissionScope cast, out bool result)
    {
        var operands = test switch
        {
            AbilityCondition.All all => all.Operands,
            AbilityCondition.Any any => any.Operands,
            _ => default,
        };
        if (!operands.IsDefault)
        {
            var values = new List<bool>();
            foreach (var child in operands)
            {
                if (!TryPowerTest(child, schemeThreat, cast, out bool value))
                {
                    result = false;
                    return false;
                }
                values.Add(value);
            }
            result = test is AbilityCondition.All ? values.All(value => value) : values.Any(value => value);
            return true;
        }
        if (test is AbilityCondition.Negated negated)
        {
            if (!TryPowerTest(negated.Operand, schemeThreat, cast, out bool value))
            {
                result = false;
                return false;
            }
            result = !value;
            return true;
        }
        if (test is AbilityCondition.AtLeast comparison
            && TryPowerAmount(comparison.Value, schemeThreat, cast, out long valueAt)
            && TryPowerAmount(comparison.Count, schemeThreat, cast, out long count))
        {
            result = valueAt >= count;
            return true;
        }
        if (!ReadsChangedThreat(test, schemeThreat, cast))
        {
            result = Test(test, cast);
            return true;
        }
        result = false;
        return false;
    }

    internal static bool TryPowerAmount(
        AbilityNumber number, Dictionary<int, long> schemeThreat,
        AbilityAdmissionScope cast, out long amount)
    {
        if (!ReadsChangedThreat(number, schemeThreat, cast))
        {
            amount = Amount(number, cast);
            return true;
        }
        if (number is AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Threat } value
            && Find(value.Card, cast) is { } scheme)
        {
            amount = TraceThreat(schemeThreat, scheme);
            return true;
        }
        amount = 0;
        return false;
    }

    internal static bool ReadsChangedThreat(
        AbilityNumber number, Dictionary<int, long> schemeThreat, AbilityAdmissionScope cast) => number switch
        {
            AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Threat } value =>
                Find(value.Card, cast) is { } scheme && schemeThreat.ContainsKey(scheme.ObjectId),
            AbilityNumber.Sum sum => sum.Operands.Any(value => ReadsChangedThreat(value, schemeThreat, cast)),
            AbilityNumber.FloorDivide division => division.Operands.Any(value => ReadsChangedThreat(value, schemeThreat, cast)),
            AbilityNumber.Minimum minimum => minimum.Operands.Any(value => ReadsChangedThreat(value, schemeThreat, cast)),
            AbilityNumber.Product product => product.Operands.Any(value => ReadsChangedThreat(value, schemeThreat, cast)),
            AbilityNumber.Conditional conditional => ReadsChangedThreat(conditional.Test, schemeThreat, cast)
                || ReadsChangedThreat(conditional.Then, schemeThreat, cast) || ReadsChangedThreat(conditional.Else, schemeThreat, cast),
            _ => false,
        };

    internal static bool ReadsChangedThreat(
        AbilityCondition condition, Dictionary<int, long> schemeThreat, AbilityAdmissionScope cast) => condition switch
        {
            AbilityCondition.All all => all.Operands.Any(test => ReadsChangedThreat(test, schemeThreat, cast)),
            AbilityCondition.Any any => any.Operands.Any(test => ReadsChangedThreat(test, schemeThreat, cast)),
            AbilityCondition.Negated negated => ReadsChangedThreat(negated.Operand, schemeThreat, cast),
            AbilityCondition.AtLeast comparison => ReadsChangedThreat(comparison.Value, schemeThreat, cast)
                || ReadsChangedThreat(comparison.Count, schemeThreat, cast),
            _ => false,
        };

}
