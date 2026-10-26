using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Checks an event body against feasible declared resource receipts before costs commit.</summary>
internal static class AbilityPaymentReceiptAdmission
{
    internal static AbilityAdmissionResult? Admit(CompiledCardAbility ability,
        AbilityAdmissionContext context, IReadOnlyList<int>? selected)
    {
        if (!Applies(ability, context))
            return AbilityDiscardReceiptAdmission.Admit(ability, context, selected);
        var price = AbilityPaymentPricing.CombinedPrice(context.World, context.Source,
            context.Query.Player, ability, context.ResourceAbilities)!;
        int cost = int.Parse(price.Cost, System.Globalization.CultureInfo.InvariantCulture);
        string generated = string.Concat(price.Generators.Select(source => source.Generates));
        foreach (string receipt in Receipts(generated, cost))
        {
            if (!Resources.Pays(receipt, cost, string.Concat(price.Rule ?? []))) continue;
            var result = AbilityDiscardReceiptAdmission.Admit(ability, WithReceipt(context, receipt), selected);
            if (result is not null) return result;
        }
        return null;
    }

    internal static void Validate(CompiledCardAbility ability, AbilityAdmissionContext context, string receipt)
    {
        if (ability.Labels.Length > 0 && LabeledAbilities.WouldBeCancelled(
                context.World, context.World.Facts, AbilityCardQueries.Resolver(context.Query),
                context.Source, ability.Labels)) return;
        if (Applies(ability, context)
            && AbilityDiscardReceiptAdmission.Admit(ability, WithReceipt(context, receipt), null) is null)
            throw new RulesNotImplementedException("the selected event payment has no valid effect target");
    }

    private static bool Applies(CompiledCardAbility ability, AbilityAdmissionContext context) =>
        ability.Cost is null && EffectiveCards.Kind(context.Source, context.World.Facts) == CardKind.Event
        && AbilityEffectStructure.PaidResourceQueries(ability.Effect).Any();

    private static AbilityAdmissionContext WithReceipt(AbilityAdmissionContext context, string receipt) =>
        context with
        {
            Expressions = context.Expressions with { Payment = receipt },
            Reachability = context.Reachability with { PaymentReceiptKnown = true },
        };

    // Engine choice: enumerate distinct resource multisets, not source subsets.
    // Unused icons model overpayment; wild declarations are explicit resource types.
    private static IEnumerable<string> Receipts(string generated, int cost)
    {
        var receipts = new HashSet<string>(StringComparer.Ordinal) { string.Empty };
        foreach (char icon in generated)
        {
            var next = new HashSet<string>(receipts, StringComparer.Ordinal);
            foreach (string receipt in receipts.Where(value => value.Length < cost))
                foreach (char type in icon == Resources.Wild ? Resources.Types : [icon])
                    next.Add(new string((receipt + type).Order().ToArray()));
            receipts = next;
        }
        return receipts.Where(receipt => receipt.Length == cost).Order(StringComparer.Ordinal);
    }
}
