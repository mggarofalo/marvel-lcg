using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Adds a bonus or overkill to the exact basic power use that triggered the ability.</summary>
internal static class AbilityBasicUseBoost
{
    internal static AbilityImmediateResult Run(AbilityEffect.BoostBasicUse bonus, AbilityImmediateContext context)
    {
        var use = context.Admission.Query.Occurrence.BasicUse
            ?? throw new RulesNotImplementedException("basic-use bonus requires its exact occurrence");
        if (!use.IsCurrent(context.Admission.World)) return new(true, false);
        if (bonus.Overkill)
        {
            use.GrantOverkill();
            return new(true, true);
        }
        long amount = bonus.MatchingAlly
            ? BasicUseAllyReceipt.Read(context.Admission.Expressions.Results, context.Admission.World, use)
            : bonus.Variable is { } variable ? context.Admission.Evaluator().Amount(variable) : bonus.Amount;
        if (amount <= 0) return new(true, false);
        use.Increase(amount);
        return new(true, true);
    }
}
