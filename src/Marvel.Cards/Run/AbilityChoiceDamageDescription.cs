using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityStructuralQueries;

namespace Marvel.Cards.Run;

/// <summary>Describes the currently knowable damage of an admitted target choice.</summary>
internal static class AbilityChoiceDamageDescription
{
    internal static string? Description(AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card card) =>
        ProjectedDamage(context, choice.Effect, publicPath: AbilityPublicChoicePath.AllowsAmount(context, choice)) is { } projection ? DescribeDamage(context, choice, projection, card) : null;

    private static string DescribeDamage(AbilityStructuralContext context, AbilityEffect.ChooseCard choice,
        (AbilityNumber Amount, bool IsAttack, bool Overkill, bool PublicPath, int? Maximum) projection, Card card)
    {
        var world = context.Expressions.World;
        string title = EffectiveCards.Title(card, world.Facts);
        if (projection.Maximum is { } maximum)
            return $"{title} · {AbilityBoundedDamageDescription.Instruction(projection.Amount, maximum)}";
        Card attacker = context.AbilityActor
            ?? world.Seats[AbilityCardQueries.Resolver(context.Expressions.Bindings)].IdentityCard;
        if (projection.IsAttack
            && Statuses.Afflicted(world, world.Facts, attacker, Statuses.Stunned))
        {
            return $"{title} · Stunned cancels this attack; no damage will be dealt";
        }

        long amount = ProjectedDamageAmount(context, projection.Amount, projection.IsAttack);
        string consequence = projection.IsAttack
            ? Damage.PreviewAttack(world, world.Facts, attacker, context.Expressions.Source, card,
                amount, projection.Overkill)
            : Damage.PreviewDamage(world, world.Facts, context.Expressions.Source, card, amount);
        return $"{title} · {CurrentAmount(context, choice, projection.Amount, amount, projection.IsAttack, projection.PublicPath)}{consequence}";
    }

    private static string CurrentAmount(AbilityStructuralContext context, AbilityEffect.ChooseCard choice,
        AbilityNumber instruction, long amount, bool attack, bool publicPath)
    {
        // Describe an uncapped quantity only when its expression is public.
        // A capped HP preview is not permission to expose a concealed count.
        if (!publicPath || !(AbilityPublicAmounts.IsCurrentStep(instruction)
            || AbilitySourceThreatDescription.AllowsAmount(context, choice, instruction))) return string.Empty;
        string kind = attack ? "attack damage" : "damage";
        return $"Current {kind}: {amount} before prevention/replacement. "
            + "Later effects may change it. ";
    }

    private static (AbilityNumber Amount, bool IsAttack, bool Overkill, bool PublicPath, int? Maximum)? ProjectedDamage(
        AbilityStructuralContext context, AbilityEffect? effect, bool attack = false, bool publicPath = true)
    {
        if (effect is AbilityEffect.Power { Kind: AbilityPowerKind.Attack } power)
            return ProjectedDamage(context, power.Effect, attack: true, publicPath);
        if (effect is AbilityEffect.Conditional conditional)
            return ProjectedDamage(context,
                Test(conditional.Test, context.Expressions)
                    ? conditional.Then : conditional.Else, attack,
                publicPath && AbilityPublicAmounts.IsCurrentStep(conditional.Test));
        if (effect is AbilityEffect.Sequence sequence)
            return ProjectedDamage(context, sequence.Effects.FirstOrDefault(), attack, publicPath);
        return effect switch
        {
            AbilityEffect.AttackDamage damage => (damage.Amount, true, damage.Overkill, publicPath, null),
            AbilityEffect.Damage damage => (damage.Amount, attack, false, publicPath, damage.Maximum),
            _ => null,
        };
    }

    private static long ProjectedDamageAmount(
        AbilityStructuralContext context, AbilityNumber damage, bool attack)
    {
        var world = context.Expressions.World;
        long amount = AbilityAmounts.SaturatingSum(
            Amount(damage, context.Expressions),
            [AbilityEventModifiers.Amount(world, context.Expressions.Source, "eventDamage")]);
        return attack
            ? AbilityAmounts.SaturatingSum(amount,
                [AbilityEventModifiers.Amount(world, context.Expressions.Source, "attackDamage")])
            : amount;
    }

}
