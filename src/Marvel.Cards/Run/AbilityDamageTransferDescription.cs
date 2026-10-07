using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Public meaning of damage transferred from the resolving identity to a chosen card.</summary>
internal static class AbilityDamageTransferDescription
{
    internal static string? Question(AbilityStructuralContext context, AbilityEffect.ChooseCard choice) =>
        Instruction(choice.Effect) is null ? null
            : $"{context.Expressions.World.Facts.Title(context.SourceFace)}: choose where to move damage";

    internal static string? Summary(AbilityStructuralContext context, AbilityEffect.ChooseCard choice)
    {
        if (Instruction(choice.Effect) is not { } move) return null;
        long limit = AbilityStructuralQueries.Amount(move.Amount, context.Expressions);
        Card hero = Identity(context);
        return $"Move up to {limit} damage from {EffectiveCards.Title(hero, context.Expressions.World.Facts)} "
            + $"to the chosen character ({hero.Damage} damage available). "
            + "Prevention and later effects can change the result.";
    }

    internal static string? Commitment(AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card candidate)
    {
        if (Instruction(choice.Effect) is not { } move) return null;
        long amount = Math.Min(Identity(context).Damage,
            AbilityStructuralQueries.Amount(move.Amount, context.Expressions));
        return $"Move {amount} damage to {EffectiveCards.Title(candidate, context.Expressions.World.Facts)}";
    }

    internal static string? Description(AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card candidate)
    {
        if (Instruction(choice.Effect) is not { } move) return null;
        var world = context.Expressions.World;
        Card hero = Identity(context);
        Card attacker = context.AbilityActor ?? hero;
        if (move.Attack && Statuses.Afflicted(world, world.Facts, attacker, Statuses.Stunned))
            return "Stunned cancels this attack; no damage moves.";
        long amount = Math.Min(hero.Damage, AbilityStructuralQueries.Amount(move.Amount, context.Expressions));
        string result = move.Attack
            ? Damage.PreviewAttack(world, world.Facts, attacker, context.Expressions.Source, candidate, amount)
            : Damage.PreviewDamage(world, world.Facts, context.Expressions.Source, candidate, amount);
        return $"Move {amount} damage from {EffectiveCards.Title(hero, world.Facts)}. {result}";
    }

    private static Card Identity(AbilityStructuralContext context) =>
        context.Expressions.World.Seats[AbilityCardQueries.Resolver(context.Expressions.Bindings)].IdentityCard;

    private static AbilityEffect.MoveDamage? Instruction(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Power { Kind: AbilityPowerKind.Attack } power => Instruction(power.Effect),
        AbilityEffect.MoveDamage
        {
            From: AbilityCardSelection.Bound { Binding: AbilityCardBinding.You },
            To: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
        } move when AbilityPublicAmounts.IsCurrentStep(move.Amount) => move,
        _ => null,
    };
}
