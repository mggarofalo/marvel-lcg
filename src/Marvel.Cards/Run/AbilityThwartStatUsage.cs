using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Reports stat dependencies from compiled power text to the rules scheduler.</summary>
internal static class AbilityThwartStatUsage
{
    internal static bool ReadsActor(AbilityEffect effect, AbilityQueryContext context, Card actor)
    {
        var selectors = new AbilitySelectorEvaluation(context);
        return Reads(effect).Any(selection => selectors.Find(selection)?.ObjectId == actor.ObjectId);
    }

    private static IEnumerable<AbilityCardSelection> Reads(AbilityEffect effect) =>
        AbilityEffectStructure.EffectNumbers(effect).SelectMany(Reads)
            .Concat(AbilityEffectStructure.EffectConditions(effect).SelectMany(Reads))
            .Concat(AbilityEffectStructure.AllEffectChildren(effect).SelectMany(Reads));

    private static IEnumerable<AbilityCardSelection> Reads(AbilityNumber number) => number switch
    {
        AbilityNumber.Modified { Field: "thwart" } modified => [modified.Card],
        AbilityNumber.Sum sum => sum.Operands.SelectMany(Reads),
        AbilityNumber.Product product => product.Operands.SelectMany(Reads),
        AbilityNumber.Minimum minimum => minimum.Operands.SelectMany(Reads),
        AbilityNumber.FloorDivide divide => divide.Operands.SelectMany(Reads),
        AbilityNumber.Conditional conditional => Reads(conditional.Test)
            .Concat(Reads(conditional.Then)).Concat(Reads(conditional.Else)),
        _ => [],
    };

    private static IEnumerable<AbilityCardSelection> Reads(AbilityCondition condition) => condition switch
    {
        AbilityCondition.All all => all.Operands.SelectMany(Reads),
        AbilityCondition.Any any => any.Operands.SelectMany(Reads),
        AbilityCondition.Negated negated => Reads(negated.Operand),
        AbilityCondition.AtLeast comparison => Reads(comparison.Value).Concat(Reads(comparison.Count)),
        _ => [],
    };
}
