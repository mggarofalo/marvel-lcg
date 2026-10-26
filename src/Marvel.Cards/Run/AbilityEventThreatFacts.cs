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
        return Reachable(effect, execution.AdmissionContext(cast), true);
    }

    internal static void Validate(AbilityResolutionExecution execution, AbilityResolutionState cast,
        CompiledCardAbility ability)
    {
        if (EffectiveCards.Kind(cast.Source, cast.World.Facts) != CardKind.Event) return;
        if (!HasLiveModifier(execution, cast)) return;
        _ = Reachable(ability.Effect, execution.AdmissionContext(cast), true);
    }

    private static bool HasLiveModifier(AbilityResolutionExecution execution, AbilityResolutionState cast) =>
        cast.World.Cards.Any(card => card.Ready && card.Owner == cast.Player
            && card.Area.Type is DeckType.UpgradesArea or DeckType.SupportsArea
            && execution.program.On(card.FaceId).Any(candidate => HasModifier(candidate.Effect)));

    private static bool HasModifier(AbilityEffect effect) =>
        effect is AbilityEffect.ModifyPlayedEvent { Kind: "eventThreatRemoval" }
        || AbilityEffectStructure.AllEffectChildren(effect).Any(HasModifier);

    private static bool ContainsRemoval(AbilityEffect effect) => effect is AbilityEffect.RemoveThreat
        || AbilityEffectStructure.AllEffectChildren(effect).Any(ContainsRemoval);

    private static bool Reachable(AbilityEffect effect, AbilityAdmissionContext context, bool beforeMutation)
    {
        if (!ContainsRemoval(effect)) return false;
        if (effect is AbilityEffect.RemoveThreat) return true;
        if (effect is AbilityEffect.Conditional conditional)
            return ConditionalRemoval(conditional, context, beforeMutation);
        if (effect is AbilityEffect.Sequence sequence)
            return SequenceRemoval(sequence, context, beforeMutation);
        if (effect is AbilityEffect.Power power) return Reachable(power.Effect, context, beforeMutation);
        if (effect is AbilityEffect.ChooseCard choice) return Reachable(choice.Effect, context, false);
        throw Unsupported();
    }

    private static bool ConditionalRemoval(AbilityEffect.Conditional conditional,
        AbilityAdmissionContext context, bool beforeMutation)
    {
        if (!beforeMutation || AbilityBindingAnalysis.BindingCanChange(conditional.Test))
            throw Unsupported();
        var branch = context.Evaluator().Test(conditional.Test) ? conditional.Then : conditional.Else;
        return branch is not null && Reachable(branch, context, true);
    }

    private static bool SequenceRemoval(AbilityEffect.Sequence sequence,
        AbilityAdmissionContext context, bool beforeMutation)
    {
        bool found = false;
        for (int index = 0; index < sequence.Effects.Length; index++)
            found |= Reachable(sequence.Effects[index], context, beforeMutation && index == 0);
        return found;
    }

    private static RulesNotImplementedException Unsupported() => new(
        "paid-event threat modifiers cannot determine the reachable removal in this effect composition before costs");
}
