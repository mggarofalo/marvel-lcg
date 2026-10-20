using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Admits a hand-discard-dependent effect against its possible public cost receipts.</summary>
internal static class AbilityDiscardReceiptAdmission
{
    internal static AbilityAdmissionResult? Admit(AbilityEffect effect, AbilityCost? cost,
        AbilityAdmissionContext context, IReadOnlyList<int>? selected)
    {
        if (!HasSingleDiscard(cost)) return Accepted(effect, context);
        var hand = AbilityCostSelection.HandCandidates(context.World, context.Source, context.Query.Player);
        IEnumerable<Card> candidates = selected is null ? hand
            : selected.Count == 1 ? hand.Where(card => card.ObjectId == selected[0]) : [];
        foreach (var card in candidates)
        {
            var candidate = context with
            {
                Expressions = context.Expressions with { Discarded = [card] },
            };
            if (Accepted(effect, candidate) is { } result) return result;
        }
        return null;
    }

    private static AbilityAdmissionResult? Accepted(AbilityEffect effect, AbilityAdmissionContext context)
    {
        var result = AbilityAdmission.Admit(effect, context);
        return result.IsAdmissible ? result : null;
    }

    private static bool HasSingleDiscard(AbilityCost? cost)
    {
        var costs = Flatten(cost).ToArray();
        return costs.OfType<AbilityCost.DiscardFromHand>().ToArray() is
            [{ Range: AbilityCostRange.Exact { Count: 1 } }]
            && !costs.OfType<AbilityCost.ExhaustChosen>().Any();
    }

    private static IEnumerable<AbilityCost> Flatten(AbilityCost? cost) => cost switch
    {
        AbilityCost.Sequence sequence => sequence.Costs.SelectMany(Flatten),
        null => [],
        _ => [cost],
    };
}
