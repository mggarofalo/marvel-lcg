using static Marvel.Cards.Run.AbilityPowerSuspension;
using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionAdmissionAdapter
{
    // The interpreter translates its resolving frame into immutable admission
    // facts, then applies only the evidence returned by the independent owner.
    internal static AbilityAdmissionContext AdmissionContext(this AbilityResolutionExecution execution, AbilityResolutionState cast) =>
        new(
            execution.program, execution.resourceAbilities,
            cast.ExpressionContext(), cast.Reachability, cast.Power,
            cast.HasContinuation);

    // The executor owns this one-way snapshot boundary. Structural execution
    // receives values, never AbilityResolutionState or an execution callback.
    internal static AbilityStructuralContext StructuralContext(this AbilityResolutionExecution execution, AbilityResolutionState cast) =>
        new(
            execution.program, execution.resourceAbilities, execution.threatAbilities,
            cast.ExpressionContext(), cast.Reachability, cast.Trigger,
            cast.Source.FaceId, cast.AbilityFace,
            cast.Player, cast.Position, cast.HasContinuation, cast.Tier,
            cast.Power, cast.AbilityActor, cast.HasPendingDependency,
            cast.ValidatedCrisisIgnoringThwarts, cast.RestoredCrisisIgnoringThwarts,
            [.. cast.StructuralPath]);

    internal static bool CanInitiate(this AbilityResolutionExecution execution, CompiledCardAbility ability, AbilityResolutionState cast)
    {
        var context = execution.AdmissionContext(cast).WithReachability(
            cast.Reachability with { CheckingInitiation = true });
        if (!AbilityInitiation.LabelsCanInitiate(ability, context))
        {
            return false;
        }

        cast.LabelsPreflighted = true;
        if (ability.Labels.Length > 0
            && Marvel.Rules.Play.LabeledAbilities.WouldBeCancelled(
                cast.World, cast.World.Facts, execution.Resolver(cast),
                cast.Source, ability.Labels))
        {
            return true;
        }

        var admission = ability.Cost is AbilityCost.SpendEnergy
            ? AbilityPaymentReceiptAdmission.Admit(ability, context, null)
            : AbilityEntryAdmission.Admit(ability.Effect, context);
        return admission is not null && execution.ApplyAdmission(admission, cast);
    }

    internal static bool CanInitiate(this AbilityResolutionExecution execution, AbilityEffect effect, AbilityResolutionState cast) =>
        execution.ApplyAdmission(AbilityEntryAdmission.Admit(effect, execution.AdmissionContext(cast)), cast);

    internal static bool BindingCanChange(this AbilityResolutionExecution execution, AbilityEffect? effect) =>
        AbilityBindingAnalysis.BindingCanChange(effect);

    internal static bool BindingCanChange(this AbilityResolutionExecution execution, AbilityPlayerSelection players) =>
        AbilityBindingAnalysis.BindingCanChange(players);

    internal static bool SuspendsPowerEffect(this AbilityResolutionExecution execution,
        AbilityEffect effect, AbilityResolutionState cast, bool stateMayChange = false,
        bool bindingMayChange = false) =>
        AbilityPowerSuspension.SuspendsPowerEffect(
            effect, execution.AdmissionContext(cast), stateMayChange, bindingMayChange);

    internal static IEnumerable<AbilityEffect> MutationChildren(this AbilityResolutionExecution execution, AbilityEffect effect) =>
        AbilityRepeatedStatusTrace.MutationChildren(effect);
    internal static IEnumerable<AbilityEffect> ContinuationChildren(this AbilityResolutionExecution execution, AbilityEffect effect) =>
        AbilityRepeatedStatusTrace.ContinuationChildren(effect);
    internal static IEnumerable<AbilityEffect> EachPlayers(this AbilityResolutionExecution execution, AbilityEffect effect) =>
        AbilityPowerStateProjection.EachPlayers(effect);
    internal static bool ContainsEffect(this AbilityResolutionExecution execution, AbilityEffect effect, string kind) =>
        AbilityDamageProtectionTrace.ContainsEffect(effect, kind);
    internal static long SoakDiscardThreshold(this AbilityResolutionExecution execution, AbilityEffect effect) =>
        AbilityDamageProtectionTrace.SoakDiscardThreshold(effect);
    internal static long SaturatingSum(this AbilityResolutionExecution execution, long own, IEnumerable<long> rest) =>
        AbilityRepeatedStatusTrace.SaturatingSum(own, rest);
    internal static long SaturatingMultiply(this AbilityResolutionExecution execution, long amount, long multiplier) =>
        AbilityRepeatedStatusTrace.SaturatingMultiply(amount, multiplier);
    internal static long NonNegativeForEachCount(this AbilityResolutionExecution execution, long count) =>
        AbilityEffectAdmissionConstraints.NonNegativeForEachCount(count);
    internal static long ForEachCount(this AbilityResolutionExecution execution, AbilityEffect effect, AbilityResolutionState cast) =>
        AbilityAdmission.ForEachCount(effect, execution.AdmissionContext(cast));
    internal static bool CanDraw(this AbilityResolutionExecution execution, AbilityEffect effect, AbilityResolutionState cast) =>
        AbilityAdmission.CanDraw(effect, execution.AdmissionContext(cast));
    internal static bool CanDraw(this AbilityResolutionExecution execution, Marvel.Rules.State.World world, int player) =>
        AbilityRepeatedStatusTrace.CanDraw(world, player);
    internal static bool LastingPeriodIsOpen(this AbilityResolutionExecution execution, string until, AbilityResolutionState cast) =>
        AbilityAdmission.LastingPeriodIsOpen(until, execution.AdmissionContext(cast));
    internal static void PreflightContinuationBoundaries(this AbilityResolutionExecution execution, AbilityEffect effect, AbilityResolutionState cast) =>
        AbilityAdmission.PreflightContinuationBoundaries(effect, execution.AdmissionContext(cast));
    internal static bool PriorStepCanChange(this AbilityResolutionExecution execution, AbilityCondition condition, AbilityResolutionState cast) =>
        AbilityAdmission.PriorStepCanChange(condition, execution.AdmissionContext(cast));

    internal static bool ApplyAdmission(this AbilityResolutionExecution execution, AbilityAdmissionResult result, AbilityResolutionState cast)
    {
        foreach (var thwart in result.CrisisIgnoringThwarts)
        {
            cast.ValidateCrisisIgnoringThwart(thwart);
        }
        return result.IsAdmissible;
    }
}
