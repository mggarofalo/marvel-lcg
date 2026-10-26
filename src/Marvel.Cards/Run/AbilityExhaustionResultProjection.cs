using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Refuses exhaustion receipts that the projected interpreter cannot produce.</summary>
internal static class AbilityExhaustionResultProjection
{
    internal static void Validate(IEnumerable<AbilityEffect> effects, IEnumerable<AbilityEffect> prior)
    {
        var nodes = effects.Concat(prior).SelectMany(Tree).ToArray();
        // Engine choice: fail closed for this projection shape, even where a branch
        // might avoid the read. Live resolution supplies the committed receipt.
        if (nodes.Any(node => node.OperationName() == "exhaust")
            && nodes.Any(node => AbilityEffectStructure.EffectNumbers(node).Any(Reads)
                || AbilityEffectStructure.EffectConditions(node).Any(Reads)))
            throw new RulesNotImplementedException(
                "projected exhaustion-result reads require an uncommitted exhaustion receipt");
    }

    private static IEnumerable<AbilityEffect> Tree(AbilityEffect effect) =>
        new[] { effect }.Concat(AbilityEffectStructure.AllEffectChildren(effect).SelectMany(Tree));

    private static bool Reads(AbilityNumber number) => number switch
    {
        AbilityNumber.Result { Name: "exhausted" } => true,
        AbilityNumber.Sum sum => sum.Operands.Any(Reads),
        AbilityNumber.Product product => product.Operands.Any(Reads),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(Reads),
        AbilityNumber.FloorDivide division => division.Operands.Any(Reads),
        AbilityNumber.Conditional conditional => Reads(conditional.Test)
            || Reads(conditional.Then) || Reads(conditional.Else),
        _ => false,
    };

    private static bool Reads(AbilityCondition condition) => condition switch
    {
        AbilityCondition.All all => all.Operands.Any(Reads),
        AbilityCondition.Any any => any.Operands.Any(Reads),
        AbilityCondition.Negated negated => Reads(negated.Operand),
        AbilityCondition.AtLeast comparison => Reads(comparison.Value) || Reads(comparison.Count),
        _ => false,
    };
}
