using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Known source threat for a direct damage choice, including its admitted discard cost.</summary>
internal static class AbilitySourceThreatDescription
{
    internal static string? Summary(AbilityStructuralContext context, AbilityEffect effect)
    {
        if (effect is not AbilityEffect.ChooseCard choice
            || choice.Effect is not AbilityEffect.Damage { Maximum: null } damage
            || !AllowsAmount(context, choice, damage.Amount)) return null;
        long amount = AbilityStructuralQueries.Amount(damage.Amount, context.Expressions);
        return $"{AbilityEffectDescription.Summary(choice)}. Current damage: {amount} before prevention/replacement. Later effects may change it.";
    }

    internal static bool AllowsAmount(AbilityStructuralContext context,
        AbilityEffect.ChooseCard choice, AbilityNumber amount)
    {
        if (amount is not AbilityNumber.CardValue
            { Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This },
                Property: AbilityCardNumberProperty.Threat }) return false;
        var source = context.Expressions.Source;
        var bindings = context.Expressions.Bindings;
        if (!bindings.SourceBindingIsCurrent(source)) return false;
        if (DeckTypes.IsInPlay(source.Area.Type)) return source.FaceUp;
        // Engine choice: last-known quantities are publishable only for the exact
        // direct choice whose admitted costs exhausted and discarded its public source.
        // Other departures, selectors and cost sequences remain undescribed.
        return bindings.SourceWasPublicInPlay && source.HasEnteredPlay
            && !bindings.SourceState.Ready
            && context.Program.On(context.AbilityFace).Any(ability =>
                ReferenceEquals(ability.Effect, choice) && ability.Cost is AbilityCost.Sequence
                { Costs: [AbilityCost.Exhaust { Card: AbilityCostCard.Source },
                    AbilityCost.Discard { Card: AbilityCostCard.Source }] });
    }
}
