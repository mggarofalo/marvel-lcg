using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Describes direct public consequences without evaluating concealed branch information.</summary>
internal static class AbilityOptionDescription
{
    internal static (string? Label, string? Description) From(
        AbilityStructuralContext context, AbilityEffect option)
    {
        // These admitted relations name public objects. More general selectors
        // and numeric expressions can inspect hidden content in an unchosen
        // branch; they are not evaluated merely to explain an option.
        return option switch
        {
            AbilityEffect.Damage
                { Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.You } } damage
                when PublicAmount(damage.Amount) => DamageOption(context, damage),
            AbilityEffect.PlaceThreat
                { Schemes: AbilityCardSelection.Query { Kind: AbilityCardQuery.MainScheme } } threat
                when PublicAmount(threat.Amount) => ThreatOption(context, threat),
            _ => (AbilityEffectDescription.Summary(option), null),
        };
    }

    private static bool PublicAmount(AbilityNumber amount) =>
        amount is AbilityNumber.Constant or AbilityNumber.PerPlayer;

    private static (string Label, string Description) DamageOption(
        AbilityStructuralContext context, AbilityEffect.Damage damage)
    {
        World world = context.Expressions.World;
        Card recipient = AbilityStructuralFlowExecution.Every(damage.Cards, context).Single();
        long amount = AbilityStructuralQueries.Amount(damage.Amount, context.Expressions);
        long current = AbilityAmounts.SaturatingSum(amount,
            [AbilityEventModifiers.Amount(world, context.Expressions.Source, "eventDamage")]);
        string title = EffectiveCards.Title(recipient, world.Facts);
        string preview = Damage.PreviewDamage(world, world.Facts,
            context.Expressions.Source, recipient, current);
        return ($"Take {amount} damage: {title}",
            $"{preview} Interrupts and later effects can change the result.");
    }

    private static (string Label, string Description) ThreatOption(
        AbilityStructuralContext context, AbilityEffect.PlaceThreat threat)
    {
        World world = context.Expressions.World;
        Card scheme = AbilityStructuralFlowExecution.Every(threat.Schemes, context).Single();
        long amount = AbilityStructuralQueries.Amount(threat.Amount, context.Expressions);
        long current = scheme.Tokens.GetValueOrDefault("k_threat");
        long threshold = world.Facts.PrintedValue(scheme.FaceId, "TargetThreat", world.Players);
        string state = threshold > 0 ? $"{current}/{threshold}" : current.ToString();
        return ($"Place {amount} threat on {world.Facts.Title(scheme.FaceId)}",
            $"Main scheme currently has {state} threat. Interrupts and prevention can change the placement.");
    }
}
