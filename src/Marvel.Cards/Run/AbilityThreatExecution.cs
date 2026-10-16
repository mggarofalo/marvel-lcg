using static Marvel.Cards.Run.AbilityDamageAndThreatQueries;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Executes threat changes and returns committed quantities or scheduled placements.</summary>
internal static class AbilityThreatExecution
{
    internal static void PlaceThreat(AbilityEffect.PlaceThreat threat,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        var schemes = Every(threat.Schemes, context);
        if (schemes.Count == 0) return;
        long amount = Amount(threat.Amount, context);
        if (amount <= 0) return;
        state.Threat = new ThreatBatch(schemes, context.Source, amount,
            ThreatCause.CardAbility, context.Trigger, context.Player);
        state.Suspension = AbilityDamageAndThreatSuspension.ScheduledThreat;
    }

    internal static void PreventThreat(AbilityEffect.PreventThreat prevention, AbilityDamageAndThreatContext context)
    {
        var placement = context.ImminentThreat ?? context.Occurrence.Threat
            ?? throw new RulesNotImplementedException($"'{context.Source.FaceId}' would prevent threat that is not imminent");
        placement.Prevent(Amount(prevention.Amount, context));
    }

    internal static void ReplaceThreatWithDamage(AbilityCardSelection selection, AbilityEffect node,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state)
    {
        AbilityEffectBoundary.Begin(context, state);
        var placement = context.Occurrence.Threat
            ?? throw new RulesNotImplementedException($"'{context.Source.FaceId}' would replace threat that is not imminent");
        long damage = placement.Remaining;
        var target = Find(selection, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' replaces threat with damage to a card that is not there");
        placement.Replace();
        // The replacement has applied before the nested damage procedure can
        // open a defeat window. This is the one resolution-ledger fact this
        // domain procedure must commit mid-handler.
        context.ResolveEffect();
        var request = new DamageRequest(context.Source, target, damage,
            context.Trigger, "Deal_Damage", AbilityEffectAttribution.IdentityPlayer(context))
            { RecordDefeatOn = context.World.Agenda.Occurrence };
        if (DamagePlacement.Resolve(context.World, context.World.Facts, request,
            context.Events).Outcome == Damage.Outcome.Suspended)
            state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    internal static AbilityStructuralOutcome ExecuteRemoveThreat(AbilityEffect.RemoveThreat removal,
        AbilityDamageAndThreatContext context, long multiplier = 1)
    {
        var schemes = Every(removal.Schemes, context);
        if (schemes.Count == 0) throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' would remove threat from a scheme that is not there");
        long requested = AbilityAmounts.SaturatingSum(
            AbilityAmounts.SaturatingMultiply(Amount(removal.Amount, context), multiplier),
            [AbilityEventModifiers.Amount(context.World, context.Source, "eventThreatRemoval")]);
        AbilityStructuralOutcome? outcome = null;
        var occurrence = context.World.Agenda.Occurrence;
        foreach (var scheme in schemes)
        {
            if (BlockedByCrisis(removal, context, scheme)) continue;
            long removed = Threat.Remove(context.World, context.World.Facts, context.ThreatAbilities,
                new ThreatRemovalRequest(scheme, requested, context.Trigger, "Remove_Threat",
                    AbilityEffectAttribution.IdentityPlayer(context),
                    removal.OverridesCannotFrom is { } source ? Find(source, context)?.ObjectId ?? -1 : -1)
                    { RecordDefeatOn = occurrence }, context.Events);
            if (removed > 0) outcome = AbilityCompletion.Combine(outcome,
                AbilityCompletion.Amount(removed, requested));
        }
        return outcome ?? AbilityStructuralOutcome.None;
    }

    private static bool BlockedByCrisis(AbilityEffect.RemoveThreat removal,
        AbilityDamageAndThreatContext context, Card scheme) =>
        !removal.IgnoresCrisis && scheme.Area.Type == DeckType.MainSchemesArea
        && AbilityCardQueries.IsPlayerCard(context.World.Facts, context.Source)
        && MainScheme.Crisis(context.World, context.World.Facts);
}
