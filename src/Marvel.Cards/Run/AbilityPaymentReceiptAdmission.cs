using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Checks an event body against feasible declared resource receipts before costs commit.</summary>
internal static class AbilityPaymentReceiptAdmission
{
    internal static AbilityAdmissionResult? Admit(CompiledCardAbility ability,
        AbilityAdmissionContext context, IReadOnlyList<int>? selected)
    {
        if (ability.Cost is AbilityCost.SpendEnergy)
            return AdmitEnergy(ability, context, selected);
        if (!Applies(ability, context))
            return AbilityDiscardReceiptAdmission.Admit(ability, context, selected);
        var price = AbilityPaymentPricing.CombinedPrice(context.World, context.Source,
            context.Query.Player, ability, context.ResourceAbilities)!;
        int cost = int.Parse(price.Cost, System.Globalization.CultureInfo.InvariantCulture);
        string generated = string.Concat(price.Generators.Select(source => source.PossibleResources));
        foreach (string receipt in Receipts(generated, cost))
        {
            if (!Resources.PaysDeclared(receipt, cost, string.Concat(price.Rule ?? []))) continue;
            var receiptPrice = price with { Rule = [receipt] };
            if (ResourcePayment.Allocate(receiptPrice, price.Generators.Select(source => source.Effect).ToArray()) is null)
                continue;
            var result = AbilityDiscardReceiptAdmission.Admit(ability, WithReceipt(context, receipt), selected);
            if (result is not null) return result;
        }
        return null;
    }

    internal static void Validate(CompiledCardAbility ability, AbilityAdmissionContext context, string receipt, long? energy = null)
    {
        if (ability.Labels.Length > 0 && LabeledAbilities.WouldBeCancelled(
                context.World, context.World.Facts, AbilityCardQueries.Resolver(context.Query),
                context.Source, ability.Labels)) return;
        if ((Applies(ability, context) || energy is not null)
            && AbilityDiscardReceiptAdmission.Admit(ability,
                WithReceipt(energy is { } x ? WithEnergy(context, x) : context, receipt), null) is null)
            throw new RulesNotImplementedException("the selected event payment has no valid effect target");
    }

    private static AbilityAdmissionResult? AdmitEnergy(CompiledCardAbility ability,
        AbilityAdmissionContext context, IReadOnlyList<int>? selected)
    {
        var price = AbilityPaymentPricing.CombinedPrice(context.World, context.Source,
            context.Query.Player, ability, context.ResourceAbilities)!;
        var request = price.VariableRequests.Single();
        int[] sources = [.. price.Generators.Select(source => source.Effect)];
        foreach (long x in EnergyValues(request))
        {
            var values = new Dictionary<string, long> { ["X"] = x };
            // The same component allocation used by payment prevents counting one
            // icon toward both the adjusted printed price and the energy cost.
            if (ResourcePayment.Allocate(price, sources, values) is null) continue;
            var future = WithEnergy(context, x);
            if (!AbilityEffectStructure.PaidResourceQueries(ability.Effect).Any())
            {
                if (AbilityDiscardReceiptAdmission.Admit(ability, future, selected) is { } result)
                    return result;
                continue;
            }
            int amount = checked((int)price.ResourceCosts.Sum(component => component.Cost == "X"
                ? x : long.Parse(component.Cost, System.Globalization.CultureInfo.InvariantCulture)));
            string required = string.Concat(price.ResourceCosts.Select(component =>
                ResourcePayment.RequiredResources(component, values)));
            string generated = string.Concat(price.Generators.Select(source => source.Generates));
            foreach (string receipt in Receipts(generated, amount))
            {
                // These are declarations: a wild declared as wild cannot also be energy.
                if (!Resources.PaysDeclared(receipt, amount, required)) continue;
                if (AbilityDiscardReceiptAdmission.Admit(ability, WithReceipt(future, receipt), selected)
                    is { } result) return result;
            }
        }
        return null;
    }

    private static IEnumerable<long> EnergyValues(VariableRequest request)
    {
        for (long x = request.Min; x <= request.Max; x++) yield return x;
    }

    private static AbilityAdmissionContext WithEnergy(AbilityAdmissionContext context, long energy) =>
        context with { Expressions = context.Expressions with
            { Results = context.Expressions.Results.SetItem("energy", energy) } };

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
