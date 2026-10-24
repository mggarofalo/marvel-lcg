using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Admits a hand-discard-dependent effect against its possible public cost receipts.</summary>
internal static class AbilityDiscardReceiptAdmission
{
    internal static AbilityAdmissionResult? Admit(CompiledCardAbility ability,
        AbilityAdmissionContext context, IReadOnlyList<int>? selected)
    {
        var costs = Flatten(ability.Cost).ToArray();
        var discards = costs.OfType<AbilityCost.DiscardFromHand>().ToArray();
        var exhausts = costs.OfType<AbilityCost.ExhaustChosen>().ToArray();
        if (exhausts.Length > 0)
            return Exhaustion(ability, context, exhausts, discards.Length, selected);
        if (discards.Length == 0)
            return Accepted(ability.Effect, context);
        if (discards.Length != 1)
            throw new Marvel.Rules.Play.RulesNotImplementedException("multiple hand-discard receipt commitments");
        var hand = AbilityCostSelection.HandCandidates(context.World, context.Source, context.Query.Player);
        var range = AbilityCostSelection.Range(discards[0].Range, hand.Count);
        if (selected is not null) return Selected(ability, context, hand, range, selected);
        if (discards[0].Range is AbilityCostRange.Exact { Count: 1 })
            return Single(ability, context, hand);
        if (AbilityDiscardReceiptReads.NeedsPhysicalMembers(ability.Effect))
            throw new Marvel.Rules.Play.RulesNotImplementedException(
                "multiple hand-discard admission reads physical receipt traits or icons");
        // With count-only reads, any N cards represent the effect's receipt.
        // Resource feasibility still searches the actual independent card payments.
        for (int count = range.Min; count <= Math.Min(range.Max, hand.Count); count++)
        {
            if (AcceptedReceipt(ability, context, hand.Take(count).ToArray(), null) is { } result)
                return result;
        }
        return null;
    }

    private static AbilityAdmissionResult? Exhaustion(CompiledCardAbility ability,
        AbilityAdmissionContext context, AbilityCost.ExhaustChosen[] costs, int discardCosts,
        IReadOnlyList<int>? selected)
    {
        if (costs.Length != 1 || discardCosts != 0)
            throw new Marvel.Rules.Play.RulesNotImplementedException("multiple selected cost receipts");
        return AbilityExhaustionReceiptAdmission.Admit(ability, context, costs[0], selected);
    }

    private static AbilityAdmissionResult? Selected(CompiledCardAbility ability,
        AbilityAdmissionContext context, IReadOnlyList<Card> hand, (int Min, int Max) range,
        IReadOnlyList<int> selected)
    {
        var receipt = hand.Where(card => selected.Contains(card.ObjectId)).ToArray();
        return receipt.Length != selected.Count || selected.Distinct().Count() != selected.Count
            || selected.Count < range.Min || selected.Count > range.Max
            ? null : AcceptedReceipt(ability, context, receipt, selected);
    }

    private static AbilityAdmissionResult? Single(CompiledCardAbility ability,
        AbilityAdmissionContext context, IReadOnlyList<Card> hand)
    {
        foreach (var card in hand)
            if (AcceptedReceipt(ability, context, [card], [card.ObjectId]) is { } result) return result;
        return null;
    }

    private static AbilityAdmissionResult? AcceptedReceipt(CompiledCardAbility ability,
        AbilityAdmissionContext context, Card[] receipt, IReadOnlyList<int>? selected)
    {
        if (!AbilityPaymentRules.EventPayable(context.World, context.Source, context.Query.Player,
            ability, context.ResourceAbilities, selected, receipt.Length)) return null;
        return Accepted(ability.Effect, context with
        { Expressions = context.Expressions with { Discarded = [.. receipt] } });
    }

    private static AbilityAdmissionResult? Accepted(AbilityEffect effect, AbilityAdmissionContext context)
    {
        var result = AbilityEntryAdmission.Admit(effect, context);
        return result.IsAdmissible ? result : null;
    }

    private static IEnumerable<AbilityCost> Flatten(AbilityCost? cost) => cost switch
    {
        AbilityCost.Sequence sequence => sequence.Costs.SelectMany(Flatten),
        null => [],
        _ => [cost],
    };
}
