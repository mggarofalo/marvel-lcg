using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityBookLowering;
using Marvel.Cards.Run;

namespace Marvel.Cards.Dsl;

/// <summary>Bounds the engine-chosen staged attack spelling to two constant damage sentences.</summary>
internal static class AbilityStagedAttackLowering
{
    internal static AbilityEffect Lower(AbilityValue value, AbilityLocation location, bool anyEnemy = false)
    {
        var fields = Fields(value, location, "amount");
        return new AbilityEffect.AttackAnotherEnemy(
            PositiveInteger(Required(fields, "amount", location), location.Child("amount")), anyEnemy);
    }

    internal static bool IsBody(AbilityEffect effect) => effect is AbilityEffect.Sequence
    {
        Effects: [AbilityEffect.AttackDamage
        {
            Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            Amount: AbilityNumber.Constant, SimultaneousPlacement: false, Overkill: false,
        }, AbilityEffect.AttackAnotherEnemy { AnyEnemy: false }],
    };

    internal static bool IsSplashBody(AbilityEffect effect) => effect is AbilityEffect.Sequence
    {
        Effects: [AbilityEffect.AttackDamage
        {
            Cards: AbilityCardSelection.Query { Kind: AbilityCardQuery.Minions },
            Amount: AbilityNumber.Constant { Value: > 0 }, SimultaneousPlacement: true, Overkill: false,
        }, AbilityEffect.AttackAnotherEnemy { AnyEnemy: true }],
    };

    internal static void Validate(AbilityEffect body, AbilityPowerKind kind, AbilityCardSelection? target,
        AbilityNumber? count, AbilityLocation location)
    {
        bool contains = Walk(body).Any(node => node is AbilityEffect.AttackAnotherEnemy);
        if (contains && (kind != AbilityPowerKind.Attack || count is not null
            || !(IsBody(body) && target is AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen }
                || IsSplashBody(body) && target is null)))
            throw location.Error("attackAnotherEnemy requires one two-sentence constant-damage attack body");
    }

    internal static void ValidatePlacement(AbilityEffect effect, AbilityLocation location)
    {
        if (effect is AbilityEffect.Power power && (IsBody(power.Effect) || IsSplashBody(power.Effect))) return;
        if (effect is AbilityEffect.AttackAnotherEnemy)
            throw location.Error("attackAnotherEnemy requires one two-sentence constant-damage attack body");
        foreach (var child in AbilityEffectStructure.AllEffectChildren(effect))
            ValidatePlacement(child, location);
    }

    private static IEnumerable<AbilityEffect> Walk(AbilityEffect effect) =>
        new[] { effect }.Concat(AbilityEffectStructure.AllEffectChildren(effect).SelectMany(Walk));
}
