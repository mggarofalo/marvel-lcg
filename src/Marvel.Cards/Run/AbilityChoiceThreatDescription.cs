using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityStructuralQueries;

namespace Marvel.Cards.Run;

/// <summary>Describes threat removal and a public, target-bound thwart instruction.</summary>
internal static class AbilityChoiceThreatDescription
{
    internal static string? Description(AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card card) => choice.Effect switch
    {
        AbilityEffect.RemoveThreat threat => DirectRemoval(context, threat, card),
        AbilityEffect.Power
        {
            Kind: AbilityPowerKind.Thwart,
            Target: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            Effect: AbilityEffect.RemoveThreat
            {
                Schemes: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            } threat,
        } when AbilityPublicChoicePath.AllowsAmount(context, choice) && AbilityPublicAmounts.IsCurrentStep(threat.Amount) => Thwart(context, threat, card),
        _ => null,
    };

    private static string Thwart(AbilityStructuralContext context, AbilityEffect.RemoveThreat threat, Card card)
    {
        var world = context.Expressions.World;
        string title = EffectiveCards.Title(card, world.Facts);
        Card actor = context.AbilityActor
            ?? world.Seats[AbilityCardQueries.Resolver(context.Expressions.Bindings)].IdentityCard;
        if (Statuses.Afflicted(world, world.Facts, actor, Statuses.Confused))
            return $"{title} · Confused cancels this thwart; no threat will be removed";
        long amount = AbilityAmounts.SaturatingSum(Amount(threat.Amount, context.Expressions),
            [AbilityEventModifiers.Amount(world, context.Expressions.Source, "eventThreatRemoval")]);
        return $"{title} · Current thwart amount: {amount} threat. Restrictions and later effects can change the result.";
    }

    private static string DirectRemoval(AbilityStructuralContext context, AbilityEffect.RemoveThreat threat, Card card)
    {
        var world = context.Expressions.World;
        string title = EffectiveCards.Title(card, world.Facts);
        long current = card.Tokens.GetValueOrDefault("k_threat");
        long result = current - Math.Min(current, Amount(threat.Amount, context.Expressions));
        long threshold = world.Facts.PrintedValue(card.FaceId, "TargetThreat", world.Players);
        return threshold > 0
            ? $"{title} · {current}/{threshold} → {result}/{threshold} threat"
            : $"{title} · {current} → {result} threat";
    }
}
