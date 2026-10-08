using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Describes a bounded set of complete checked effects; unknown structure is not flattened.</summary>
internal static class AbilityPersistentEffects
{
    internal static IReadOnlyList<PersistentEffect>? Describe(AbilityEffect effect)
    {
        var found = new List<PersistentEffect>();
        return Append(effect, found) ? found.ToArray() : null;
    }

    private static bool Append(AbilityEffect effect, List<PersistentEffect> found)
    {
        if (effect is AbilityEffect.Sequence sequence)
            return sequence.Effects.All(step => Append(step, found));
        if (effect is AbilityEffect.Conditional conditional)
            return AppendConditional(conditional, found);
        PersistentEffect? described = Single(effect);
        if (described is null) return false;
        found.Add(described);
        return true;
    }

    private static PersistentEffect? Single(AbilityEffect effect) => effect switch
    {
        AbilityEffect.ChooseCard { From: AbilityCardSelection.Query { Kind: AbilityCardQuery.Schemes },
            Effect: AbilityEffect.RemoveThreat { Schemes: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
                Amount: AbilityNumber.Constant amount, IgnoresCrisis: false, OverridesCannotFrom: null } }
            => new("RemoveThreat", "ChosenScheme", amount.Value),
        AbilityEffect.CardAction action when Target(action.Selection) is { } target => Action(action.Instruction, target),
        AbilityEffect.GrantField { EachCard: false, Amount: AbilityNumber.Constant amount } grant
            when Target(grant.Cards) is { } target => new("Grant", target, amount.Value, grant.Field, grant.Until),
        AbilityEffect.DelayedDiscard delayed when Target(delayed.Card) is { } target
            => new("Discard", target, After: delayed.Condition),
        _ => null,
    };

    private static PersistentEffect? Action(AbilityCardInstruction instruction, string target)
    {
        string? operation = instruction switch
        {
            AbilityCardInstruction.Ready => "Ready",
            AbilityCardInstruction.Exhaust => "Exhaust",
            AbilityCardInstruction.Discard => "Discard",
            AbilityCardInstruction.SoakDamage => "RedirectAllDamage",
            _ => null,
        };
        return operation is null ? null : new(operation, target);
    }

    private static bool AppendConditional(AbilityEffect.Conditional conditional, List<PersistentEffect> found)
    {
        if (conditional.Else is not null || conditional.Then is null || Threshold(conditional.Test) is not { } threshold)
            return false;
        IReadOnlyList<PersistentEffect>? effects = Describe(conditional.Then);
        if (effects is null || effects.Any(item => item.Condition is not null)) return false;
        found.AddRange(effects.Select(item => item with { Condition = threshold }));
        return true;
    }

    internal static bool CoveredByValues(AbilityEffect effect) => effect switch
    {
        AbilityEffect.GrantField grant => grant.Field is "attack" or "thwart" or "defense" or "recover"
            or "scheme" or "health" or "hand_size",
        AbilityEffect.DefineBaseValue => true,
        AbilityEffect.Sequence sequence => sequence.Effects.All(CoveredByValues),
        AbilityEffect.Conditional conditional => (conditional.Then is null || CoveredByValues(conditional.Then))
            && (conditional.Else is null || CoveredByValues(conditional.Else)),
        _ => false,
    };

    private static PersistentThreshold? Threshold(AbilityCondition condition) => condition switch
    {
        AbilityCondition.AtLeast
        {
            Value: AbilityNumber.CardValue { Card: var card, Property: AbilityCardNumberProperty.Damage },
            Count: AbilityNumber.Constant minimum,
        } when Target(card) is { } target => new(target, "damage", minimum.Value),
        AbilityCondition.AtLeast
        {
            Value: AbilityNumber.Counters { Card: var card, Counter: var counter },
            Count: AbilityNumber.Constant minimum,
        } when Target(card) is { } target => new(target, counter, minimum.Value),
        _ => null,
    };

    private static string? Target(AbilityCardSelection selection) => selection switch
    {
        AbilityCardSelection.Bound bound => bound.Binding switch
        {
            AbilityCardBinding.This => "Source",
            AbilityCardBinding.AttachedTo => "Host",
            AbilityCardBinding.You => "ActingIdentity",
            AbilityCardBinding.TriggerActor => "TriggerActor",
            AbilityCardBinding.TriggerSubject => "TriggerSubject",
            AbilityCardBinding.TriggerTarget => "TriggerTarget",
            _ => null,
        },
        _ => null,
    };
}
