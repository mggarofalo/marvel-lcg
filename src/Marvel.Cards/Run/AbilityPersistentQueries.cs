using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Reads implemented persistent semantics from the checked program, never from printed prose.</summary>
internal static class AbilityPersistentQueries
{
    internal static PersistentAbilityDescription Describe(AbilityProgram program, Card card)
    {
        var found = new List<PersistentAbility>();
        bool unresolved = false;
        foreach (CompiledCardAbility ability in AbilityProgramQueries.On(program, card))
        {
            if (!PersistentTiming(ability.Trigger.Timing) || UsesEffectiveValues(ability)) continue;
            IReadOnlyList<PersistentCost>? costs = AbilityPersistentCosts.Describe(ability.Cost);
            IReadOnlyList<PersistentEffect>? effects = AbilityPersistentEffects.Describe(ability.Effect);
            if (!SupportedEnvelope(ability) || costs is null || effects is null)
            {
                unresolved = true;
                continue;
            }
            AbilityTrigger trigger = ability.Trigger;
            found.Add(new(new(trigger.Timing.ToString(), trigger.Event, trigger.Subject, trigger.Actor,
                trigger.Target, trigger.Form, trigger.Player, ability.AnyPlayer), costs, effects));
        }
        return new(found.ToArray(), unresolved, HasAttachmentInstruction(program, card));
    }

    private static bool PersistentTiming(AbilityType timing) =>
        timing is not (AbilityType.WhenRevealed or AbilityType.Setup or AbilityType.Boost);

    private static bool UsesEffectiveValues(CompiledCardAbility ability) =>
        ability.When is null && ability.Trigger.Timing == AbilityType.Constant
        && AbilityPersistentEffects.CoveredByValues(ability.Effect);

    private static bool HasAttachmentInstruction(AbilityProgram program, Card card) =>
        // The checked identity-limit selector implements "play under the control"
        // placement. Its hosted storage is not an attach-to instruction.
        !EffectiveCards.HasProfile(card)
        && program.AttachTo.TryGetValue(card.FaceId, out AbilityCardSelection? placement)
        && placement is not AbilityCardSelection.Query { Kind: AbilityCardQuery.IdentitiesWithinPerPlayerLimit };

    private static bool SupportedEnvelope(CompiledCardAbility ability) => ability.When is null
        && ability.Limit is null && ability.Maximum is null && ability.Trigger.Also is null && ability.Labels.IsEmpty;

}
