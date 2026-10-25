using System.Collections.Immutable;
using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Admits an effect against legal physical exhaustion cost receipts.</summary>
internal static class AbilityExhaustionReceiptAdmission
{
    internal static AbilityAdmissionResult? Admit(CompiledCardAbility ability,
        AbilityAdmissionContext context, AbilityCost.ExhaustChosen cost, IReadOnlyList<int>? selected)
    {
        int[] candidates = [.. AbilityCostSelection.Choices(context.World, context.Query.Player, cost)
            .Where(card => card.Ready).Select(card => card.ObjectId)];
        var range = AbilityCostSelection.Range(cost.Range, candidates.Length);
        if (selected is not null)
        {
            if (selected.Count < range.Min || selected.Count > range.Max
                || selected.Distinct().Count() != selected.Count
                || selected.Any(id => !candidates.Contains(id))) return null;
            return Accepted(ability, context, selected);
        }
        // Stable candidate order is an engine choice. Each physical subset is
        // checked because an effect can exclude the characters exhausted for cost.
        for (int count = range.Min; count <= Math.Min(range.Max, candidates.Length); count++)
            foreach (var receipt in Subsets(candidates, count, 0, []))
                if (Accepted(ability, context, receipt) is { } result) return result;
        return null;
    }

    private static AbilityAdmissionResult? Accepted(CompiledCardAbility ability,
        AbilityAdmissionContext context, IReadOnlyList<int> receipt)
    {
        var projected = context.WithQuery(context.Query with
        { ExhaustedForCost = receipt.ToImmutableHashSet() });
        var result = AbilityEntryAdmission.Admit(ability.Effect, projected);
        return result.IsAdmissible ? result : null;
    }

    private static IEnumerable<int[]> Subsets(int[] candidates, int remaining, int start, int[] prefix)
    {
        if (remaining == 0)
        {
            yield return prefix;
            yield break;
        }
        for (int index = start; index <= candidates.Length - remaining; index++)
            foreach (var receipt in Subsets(candidates, remaining - 1, index + 1, [.. prefix, candidates[index]]))
                yield return receipt;
    }
}
