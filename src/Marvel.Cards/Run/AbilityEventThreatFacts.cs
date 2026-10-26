using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Threat-removal facts for one selected event body and its current conditional branch.</summary>
internal static class AbilityEventThreatFacts
{
    internal static bool RemovesThreat(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        if (!HasLiveModifier(execution, cast)) return false;
        var address = cast.ResolutionAbility
            ?? throw new RulesNotImplementedException("paid event has no selected ability");
        var effect = execution.Pending(cast.Source, address).Effect;
        return Reachable(effect, execution.AdmissionContext(cast), true, false);
    }

    internal static void Validate(AbilityResolutionExecution execution, AbilityResolutionState cast,
        CompiledCardAbility ability)
    {
        if (EffectiveCards.Kind(cast.Source, cast.World.Facts) != CardKind.Event) return;
        if (!HasLiveModifier(execution, cast)) return;
        _ = Reachable(ability.Effect, execution.AdmissionContext(cast), true, true);
    }

    private static bool HasLiveModifier(AbilityResolutionExecution execution, AbilityResolutionState cast) =>
        cast.World.Cards.Any(card => card.Ready && card.Owner == cast.Player
            && card.Area.Type is DeckType.UpgradesArea or DeckType.SupportsArea
            && AbilityProgramQueries.ActiveOn(cast.World, execution.program, card)
                .Any(candidate => HasModifier(candidate.Effect)));

    private static bool HasModifier(AbilityEffect effect) =>
        effect is AbilityEffect.ModifyPlayedEvent { Kind: "eventThreatRemoval" }
        || AbilityEffectStructure.AllEffectChildren(effect).Any(HasModifier);

    private static bool ContainsRemoval(AbilityEffect effect) => effect is AbilityEffect.RemoveThreat
        || AbilityEffectStructure.AllEffectChildren(effect).Any(ContainsRemoval);

    private static bool Reachable(AbilityEffect? effect, AbilityAdmissionContext context,
        bool beforeMutation, bool beforePayment)
    {
        if (effect is null || !ContainsRemoval(effect)) return false;
        if (effect is AbilityEffect.RemoveThreat) return true;
        if (effect is AbilityEffect.Conditional conditional)
            return ConditionalRemoval(conditional, context, beforeMutation, beforePayment);
        if (effect is AbilityEffect.Sequence sequence)
            return SequenceRemoval(sequence, context, beforeMutation, beforePayment);
        if (effect is AbilityEffect.Power power) return Reachable(power.Effect, context, beforeMutation, beforePayment);
        if (effect is AbilityEffect.ChooseCard choice) return Reachable(choice.Effect, context, false, beforePayment);
        throw Unsupported();
    }

    private static bool ConditionalRemoval(AbilityEffect.Conditional conditional,
        AbilityAdmissionContext context, bool beforeMutation, bool beforePayment)
    {
        if (!beforeMutation || AbilityBindingAnalysis.BindingCanChange(conditional.Test))
            throw Unsupported();
        // Engine choice: before costs, inspect both possible payment-dependent
        // branches. The empty receipt cannot identify the branch after payment.
        // Once costs settle, initiation facts use only the actual branch.
        if (beforePayment && AbilityAdmissionResolutionPreflight.PaymentCanChange(conditional.Test))
            return Reachable(conditional.Then, context, true, true)
                | (conditional.Else is not null && Reachable(conditional.Else, context, true, true));
        var branch = context.Evaluator().Test(conditional.Test) ? conditional.Then : conditional.Else;
        return branch is not null && Reachable(branch, context, true, beforePayment);
    }

    private static bool SequenceRemoval(AbilityEffect.Sequence sequence,
        AbilityAdmissionContext context, bool beforeMutation, bool beforePayment)
    {
        bool found = false;
        for (int index = 0; index < sequence.Effects.Length; index++)
            found |= Reachable(sequence.Effects[index], context, beforeMutation && index == 0, beforePayment);
        return found;
    }

    private static RulesNotImplementedException Unsupported() => new(
        "paid-event threat modifiers cannot determine the reachable removal in this effect composition before costs");
}
