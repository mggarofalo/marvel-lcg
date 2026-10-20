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
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state, long multiplier = 1)
    {
        state.SchemesCleared = 0;
        if (removal.Assigned) return AbilityThreatDistribution.Remove(context, state);
        var schemes = Every(removal.Schemes, context);
        if (schemes.Count == 0) throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' would remove threat from a scheme that is not there");
        long requested = AbilityAmounts.SaturatingSum(
            AbilityAmounts.SaturatingMultiply(Amount(removal.Amount, context), multiplier),
            [AbilityEventModifiers.Amount(context.World, context.Source, "eventThreatRemoval")]);
        return RemoveFromSchemes(removal, context, state,
            schemes.Where(scheme => !BlockedByCrisis(removal, context, scheme)).ToArray(), requested);
    }

    private static AbilityStructuralOutcome RemoveFromSchemes(AbilityEffect.RemoveThreat removal,
        AbilityDamageAndThreatContext context, AbilityDamageAndThreatState state,
        IReadOnlyList<Card> schemes, long requested)
    {
        var owner = context.World.Agenda.Current;
        var occurrence = context.World.Agenda.Occurrence;
        var held = schemes.Select(scheme => scheme.Tokens.GetValueOrDefault("k_threat")).ToArray();
        int overridden = removal.OverridesCannotFrom is { } source ? Find(source, context)?.ObjectId ?? -1 : -1;
        var requests = schemes.Select(scheme => new ThreatRemovalRequest(scheme, requested,
            context.Trigger, "Remove_Threat", AbilityEffectAttribution.IdentityPlayer(context), overridden)
            { RecordDefeatOn = occurrence }).ToArray();
        var results = ThreatBatchRemoval.Remove(context.World, context.ThreatAbilities, requests, context.Events);
        AbilityStructuralOutcome? outcome = null;
        for (int index = 0; index < results.Count; index++)
        {
            var result = results[index];
            RetainSuspension(state, result, owner, occurrence);
            if (result.Removed == 0) continue;
            if (result.Removed == held[index]) state.SchemesCleared++;
            outcome = AbilityCompletion.Combine(outcome, AbilityCompletion.Amount(result.Removed, requested));
        }
        return outcome ?? AbilityStructuralOutcome.None;
    }

    internal static void RetainSuspension(AbilityDamageAndThreatState state, ThreatRemovalResult result, PhaseStep? owner, Occurrence? occurrence)
    {
        if (!result.Suspended) return;
        if (state.Boundary is null && owner is { } frame && occurrence is not null)
            state.Boundary = new(frame, occurrence);
        state.Suspension = AbilityDamageAndThreatSuspension.Procedure;
    }

    private static bool BlockedByCrisis(AbilityEffect.RemoveThreat removal,
        AbilityDamageAndThreatContext context, Card scheme) =>
        !removal.IgnoresCrisis && scheme.Area.Type == DeckType.MainSchemesArea
        && AbilityCardQueries.IsPlayerCard(context.World.Facts, context.Source)
        && MainScheme.Crisis(context.World, context.World.Facts);
}
