using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Admits only outcome-independent reads of a pending random payment receipt.</summary>
internal static class AbilityRandomHandReceiptAdmission
{
    internal static AbilityAdmissionResult? Admit(CompiledCardAbility ability,
        AbilityAdmissionContext context, AbilityCost.RandomDiscardFromHand cost)
    {
        if (AbilityDiscardReceiptReads.NeedsPhysicalMembers(ability.Effect))
            throw new RulesNotImplementedException("random payment receipt admission cannot predict printed facts of the selected cards");
        var hand = AbilityCostSelection.HandCandidates(context.World, context.Source, context.Query.Player);
        if (hand.Count < cost.Count) return null;
        // Only cardinality is observable to admitted expressions. These placeholders
        // never choose the committed cards and never advance the random stream.
        var result = AbilityEntryAdmission.Admit(ability.Effect, context with
        { Expressions = context.Expressions with { Discarded = [.. hand.Take(cost.Count)] } });
        return result.IsAdmissible ? result : null;
    }
}
